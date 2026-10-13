using System.Security.Claims;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EstateFlow.Api.Services.Audit;

// POPIA audit trail. Registered globally: every authenticated API call is
// recorded with who made it, what it touched and when — including reads,
// because viewing an ID number or death certificate is also processing.
//
// Written through a separate DbContext scope so a failed request's
// half-finished changes can never be saved by accident.
public class AuditFilter : IAsyncActionFilter
{
    private static readonly string[] EstateRouteKeys = { "estateId", "targetEstateId" };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditFilter> _logger;

    public AuditFilter(IServiceScopeFactory scopeFactory, ILogger<AuditFilter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return;

        var statusCode = executed.Exception is not null && !executed.ExceptionHandled
            ? StatusCodes.Status500InternalServerError
            : context.HttpContext.Response.StatusCode;

        if (executed.Result is Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult { StatusCode: int resultStatus })
            statusCode = resultStatus;

        var entry = new AuditEntry
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name ?? "unknown",
            Action = DescribeAction(context),
            EstateId = FindEstateId(context) ?? CreatedEstateId(executed),
            Target = $"{context.HttpContext.Request.Method} {Template(context)}",
            StatusCode = statusCode
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EstateFlowDbContext>();
            db.AuditEntries.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not write audit entry for {Target}.", entry.Target);
        }
    }

    private static Guid? FindEstateId(ActionExecutingContext context)
    {
        foreach (var key in EstateRouteKeys)
        {
            if (context.RouteData.Values.TryGetValue(key, out var value) &&
                Guid.TryParse(value?.ToString(), out var id))
            {
                return id;
            }
        }

        return null;
    }

    // A newly created estate only has an id once the action has run.
    private static Guid? CreatedEstateId(ActionExecutedContext executed) =>
        executed.Result is Microsoft.AspNetCore.Mvc.CreatedAtActionResult created &&
        created.RouteValues?.TryGetValue("estateId", out var value) == true &&
        value is Guid id
            ? id
            : null;

    private static string Template(ActionExecutingContext context) =>
        context.ActionDescriptor.AttributeRouteInfo?.Template
        ?? (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName
        ?? "unknown";

    private static string DescribeAction(ActionExecutingContext context)
    {
        var template = Template(context);

        if (template.StartsWith("api/admin", StringComparison.OrdinalIgnoreCase))
            return "Admin";
        if (template.Contains("download", StringComparison.OrdinalIgnoreCase))
            return "Download";
        if (template.Contains("send-email", StringComparison.OrdinalIgnoreCase) ||
            template.Contains("portal-invites", StringComparison.OrdinalIgnoreCase))
            return "Send";
        if (template.Contains("letters", StringComparison.OrdinalIgnoreCase))
            return "Generate";

        return context.HttpContext.Request.Method.ToUpperInvariant() switch
        {
            "GET" => "View",
            "POST" => "Create",
            "PUT" or "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => context.HttpContext.Request.Method
        };
    }
}

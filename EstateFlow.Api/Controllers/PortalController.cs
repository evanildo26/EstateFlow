using System.Security.Claims;
using System.Security.Cryptography;
using EstateFlow.Api.Services.Correspondence;
using EstateFlow.Api.Services.Notifications;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Models;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EstateFlow.Api.Controllers;

// Read-only portal for executors and heirs: "where is my estate at?"
// Access is by email address: a portal user sees the estates on which they
// are recorded as an executor or beneficiary with that email.
[Authorize(Roles = "Executor,Heir")]
[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly EstateFlowDbContext _db;
    private readonly MasterProcessOptions _processOptions;

    public PortalController(EstateFlowDbContext db, IOptions<MasterProcessOptions> processOptions)
    {
        _db = db;
        _processOptions = processOptions.Value;
    }

    [HttpGet("estates")]
    public async Task<ActionResult<List<PortalEstateDto>>> GetMyEstates()
    {
        var email = (User.FindFirstValue(ClaimTypes.Email) ?? string.Empty).Trim().ToLower();
        if (email.Length == 0)
            return Ok(new List<PortalEstateDto>());

        var estates = await _db.Estates
            .AsNoTracking()
            .Include(e => e.Deceased)
            .Include(e => e.Controller)
            .Include(e => e.Executors)
            .Include(e => e.Beneficiaries)
            .Include(e => e.Milestones)
            .Include(e => e.ProgressStages)
            .AsSplitQuery()
            .Where(e =>
                e.Executors.Any(x => x.Email != null && x.Email.ToLower() == email) ||
                e.Beneficiaries.Any(x => x.Email != null && x.Email.ToLower() == email))
            .ToListAsync();

        var today = DateTime.Today;

        return Ok(estates.Select(e =>
        {
            var progress = MasterProcess.Evaluate(
                e.DateOfDeath,
                e.Milestones.GroupBy(m => m.Stage).ToDictionary(g => g.Key, g => g.First().CompletedOn),
                today,
                _processOptions);

            var roles = new List<string>();
            if (e.Executors.Any(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)))
                roles.Add("Executor");
            if (e.Beneficiaries.Any(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)))
                roles.Add("Heir / beneficiary");

            return new PortalEstateDto
            {
                EstateId = e.EstateId,
                FileNumber = e.FileNumber,
                DeceasedFullName = e.Deceased?.FullName ?? string.Empty,
                EstateStatus = EstateStatuses.Normalize(e.EstateStatus),
                YourRole = string.Join(" and ", roles),
                ControllerName = e.Controller?.StaffName,
                ControllerEmail = e.Controller?.Email,
                PercentComplete = progress.PercentComplete,
                NextStep = progress.IsFinalised ? null : progress.NextMilestone?.Title,
                Milestones = progress.Milestones.Select(m => new PortalMilestoneDto
                {
                    Title = m.Title,
                    Description = m.Description,
                    CompletedOn = m.CompletedOn,
                    IsNext = m.IsNext
                }).ToList(),
                RecentUpdates = e.ProgressStages
                    .Where(s => s.TargetDate.HasValue && s.TargetDate.Value <= today)
                    .OrderByDescending(s => s.TargetDate)
                    .Take(5)
                    .Select(s => new PortalUpdateDto { Date = s.TargetDate!.Value, Text = s.StageName })
                    .ToList()
            };
        }).ToList());
    }
}

// Staff invite an executor or heir to the portal. Lives under the estate
// route, so the usual ownership check applies.
[Authorize(Roles = "EstateController,FirmAdministrator")]
[ApiController]
[Route("api/estates/{estateId:guid}/portal-invites")]
public class PortalInvitesController : ControllerBase
{
    private static readonly string[] StaffRoles = { "EstateController", "FirmAdministrator" };

    private readonly EstateFlowDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;
    private readonly ICorrespondenceLog _correspondence;
    private readonly IConfiguration _configuration;

    public PortalInvitesController(
        EstateFlowDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications,
        ICorrespondenceLog correspondence,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _notifications = notifications;
        _correspondence = correspondence;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Invite(Guid estateId, [FromBody] InvitePortalUserRequest request)
    {
        var estate = await _db.Estates
            .AsNoTracking()
            .Include(e => e.Deceased)
            .FirstAsync(e => e.EstateId == estateId);

        (string Name, string? Email, string Role)? person = request.PersonType switch
        {
            "Executor" => await _db.Executors
                .Where(x => x.EstateId == estateId && x.ExecutorId == request.PersonId)
                .Select(x => new ValueTuple<string, string?, string>(x.FullName, x.Email, "Executor"))
                .FirstOrDefaultAsync(),
            "Beneficiary" => await _db.Beneficiaries
                .Where(x => x.EstateId == estateId && x.BeneficiaryId == request.PersonId)
                .Select(x => new ValueTuple<string, string?, string>(x.FullName, x.Email, "Heir"))
                .FirstOrDefaultAsync(),
            _ => null
        };

        if (person is null || person.Value.Name is null)
            return NotFound(new { message = "That executor or heir was not found on this estate." });

        var (name, email, role) = person.Value;

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = $"{name} has no email address recorded." });

        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => StaffRoles.Contains(r)))
                return Conflict(new { message = "This email address belongs to a staff account and cannot be used for the portal." });

            if (!roles.Contains(role))
                await _userManager.AddToRoleAsync(user, role);
        }
        else
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                EmailConfirmed = true
            };

            // Unusable random password; the person chooses their own via the link.
            var created = await _userManager.CreateAsync(user, RandomPassword());
            if (!created.Succeeded)
                return BadRequest(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(user, role);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{PortalBaseUrl()}/portal/index.html#set-password" +
                   $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        var deceased = estate.Deceased?.FullName ?? "the deceased";
        var subject = $"Your access to the estate late {deceased} (file {estate.FileNumber})";

        var body = $"""
            Dear {name},

            You can now follow the progress of the estate late {deceased} (our file {estate.FileNumber}) online.

            To choose your password, open this link within 24 hours:
            {link}

            After that, sign in at {PortalBaseUrl()}/portal/ with this email address.

            The portal shows where the estate is in the process with the Master of the High Court.
            For any questions, please contact your estate controller.

            Kind regards,
            STBB Incorporated — Deceased Estates Division
            """;

        var sent = true;
        try
        {
            await _notifications.NotifyAsync(email, subject, body);
        }
        catch (Exception)
        {
            sent = false;
        }

        await _correspondence.RecordAsync(new CorrespondenceRecord
        {
            EstateId = estateId,
            Channel = "Email",
            Kind = "Portal invitation",
            SentBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown",
            RecipientName = name,
            RecipientEmail = email,
            Subject = subject,
            Summary = "Invitation to the executor/heir progress portal.",
            Status = sent ? "Sent" : "Failed"
        });

        return sent
            ? Ok(new { message = $"Invitation emailed to {name}." })
            : StatusCode(StatusCodes.Status502BadGateway, new { message = "The invitation email could not be sent. Please try again later." });
    }

    private string PortalBaseUrl() =>
        (_configuration["Portal:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

    private static string RandomPassword() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";
}

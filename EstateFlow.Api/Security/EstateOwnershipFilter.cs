using EstateFlow.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EstateFlow.Api.Security;

// Registered globally in Program.cs.
//
// Any action whose route contains an {estateId} only runs when that estate is
// assigned to the logged-in Estate Controller (the "controllerId" JWT claim).
// Without this, any logged-in user could read, edit or delete another
// controller's estate, documents and diary just by knowing its id.
//
// A 404 is returned rather than a 403 so the API never confirms that an
// estate id belonging to someone else exists.
public class EstateOwnershipFilter : IAsyncActionFilter
{
    private const string EstateIdRouteKey = "estateId";

    private readonly IEstateRepository _repository;

    public EstateOwnershipFilter(IEstateRepository repository)
    {
        _repository = repository;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue(EstateIdRouteKey, out var rawEstateId))
        {
            await next();
            return;
        }

        if (!Guid.TryParse(rawEstateId?.ToString(), out var estateId) ||
            !context.HttpContext.User.TryGetControllerId(out var controllerId) ||
            !await _repository.IsAssignedToControllerAsync(estateId, controllerId))
        {
            context.Result = new NotFoundObjectResult(new
            {
                message = $"Estate {rawEstateId} was not found."
            });
            return;
        }

        await next();
    }
}

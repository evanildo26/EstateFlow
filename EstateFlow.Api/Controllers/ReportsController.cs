using EstateFlow.Api.Services;
using EstateFlow.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IEstateService _estateService;

    public ReportsController(IEstateService estateService)
    {
        _estateService = estateService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<List<ReportSettingDto>>> GetSettings()
    {
        var controllerIdClaim =
            User.FindFirst("controllerId")?.Value;

        if (controllerIdClaim is null ||
            !Guid.TryParse(controllerIdClaim, out var controllerId))
        {
            return Forbid();
        }

        var settings =
            await _estateService
                .GetReportSettingsAsync(controllerId);

        return Ok(settings);
    }

    [HttpPatch("settings/{estateId:guid}")]
    public async Task<ActionResult> TogglePause(Guid estateId, [FromBody] ToggleReportPauseRequest request)
    {
        try
        {
            await _estateService.ToggleReportPauseAsync(estateId, request.IsPaused);
            return Ok(new { estateId, isPaused = request.IsPaused });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
using System.Security.Claims;
using EstateFlow.Api.Services.Letters;
using EstateFlow.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/estates/{estateId:guid}/letters")]
public class LettersController : ControllerBase
{
    // Lets the desktop tell the controller whether a copy was filed.
    public const string SavedHeader = "X-EstateFlow-Letter-Filed";

    private readonly IEstateRepository _repository;
    private readonly LetterService _letters;

    public LettersController(
        IEstateRepository repository,
        LetterService letters)
    {
        _repository = repository;
        _letters = letters;
    }

    [HttpPost("vehicle/{assetId:guid}")]
    public async Task<IActionResult> GenerateVehicleLetter(
        Guid estateId,
        Guid assetId)
    {
        var estate =
            await _repository.GetByIdAsync(estateId);

        if (estate is null)
            return NotFound(new
            {
                message = "Estate not found."
            });

        var asset =
            estate.MovableAssets
                .FirstOrDefault(
                    a => a.AssetId == assetId);

        if (asset is null || !asset.IsVehicle)
        {
            return BadRequest(new
            {
                message =
                    "Movable asset not found or is not a vehicle."
            });
        }

        var result = await _letters.GenerateAsync(
            estate,
            LetterTypes.VehicleTransfer,
            asset,
            "Vehicle transfer letter",
            CurrentUserName());

        return LetterFile(result);
    }

    [HttpPost("institution/{claimId:guid}")]
    public async Task<IActionResult> GenerateInstitutionLetter(
        Guid estateId,
        Guid claimId)
    {
        var estate =
            await _repository.GetByIdAsync(estateId);

        if (estate is null)
            return NotFound(new
            {
                message = "Estate not found."
            });

        var claim =
            estate.FinancialClaims
                .FirstOrDefault(
                    c => c.ClaimId == claimId);

        if (claim is null)
        {
            return NotFound(new
            {
                message =
                    "Financial claim not found."
            });
        }

        var result = await _letters.GenerateAsync(
            estate,
            LetterTypes.InstitutionNotification,
            claim,
            "Notification of death letter",
            CurrentUserName());

        return LetterFile(result);
    }

    private FileContentResult LetterFile(LetterResult result)
    {
        Response.Headers[SavedHeader] = result.SavedToEstate ? "true" : "false";

        return File(
            result.Letter.Data,
            result.Letter.ContentType,
            result.Letter.FileName);
    }

    private string CurrentUserName() =>
        User.FindFirstValue(ClaimTypes.Name) ?? "EstateFlow";
}

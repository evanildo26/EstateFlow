using System.Security.Claims;
using EstateFlow.Api.Services.Letters;
using EstateFlow.Api.Services.Storage;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Models;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Api.Controllers;

// Firm Administrator tools: staff accounts, estate assignment, the POPIA
// audit log, letter wording and record retention.
//
// Estate routes use {targetEstateId} rather than {estateId}, because an
// administrator works across all controllers' estates and the per-controller
// ownership check does not apply here.
[Authorize(Roles = AdminRole)]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private const string AdminRole = "FirmAdministrator";
    private static readonly string[] StaffRoles = { "EstateController", AdminRole };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EstateFlowDbContext _db;
    private readonly ILetterTemplateStore _templates;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        EstateFlowDbContext db,
        ILetterTemplateStore templates,
        IBlobStorageService blobStorage,
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _db = db;
        _templates = templates;
        _blobStorage = blobStorage;
        _configuration = configuration;
        _logger = logger;
    }

    // ======================================================
    // STAFF
    // (Creating staff uses POST api/auth/register.)
    // ======================================================

    [HttpGet("staff")]
    public async Task<ActionResult<List<StaffMemberDto>>> GetStaff()
    {
        var estateCounts = await _db.Estates
            .GroupBy(e => e.ControllerId)
            .Select(g => new { ControllerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ControllerId, x => x.Count);

        var staff = new List<StaffMemberDto>();
        var seen = new HashSet<string>();

        foreach (var role in StaffRoles)
        {
            foreach (var user in await _userManager.GetUsersInRoleAsync(role))
            {
                if (!seen.Add(user.Id))
                    continue;

                var roles = await _userManager.GetRolesAsync(user);

                staff.Add(new StaffMemberDto
                {
                    UserId = user.Id,
                    ControllerId = user.ControllerId,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.Contains(AdminRole) ? AdminRole : "EstateController",
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    IsDeactivated = IsDeactivated(user),
                    IsLockedOut = !IsDeactivated(user) && user.LockoutEnd > DateTimeOffset.UtcNow,
                    AssignedEstateCount = user.ControllerId is Guid id
                        ? estateCounts.GetValueOrDefault(id)
                        : 0
                });
            }
        }

        return Ok(staff.OrderBy(s => s.FullName).ToList());
    }

    [HttpPost("staff/{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
    {
        var user = await FindStaffAsync(userId);
        if (user is null) return NotFound(new { message = "Staff member not found." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(new { message = "Password reset. Ask the staff member to sign in and change it." });
    }

    [HttpPost("staff/{userId}/unlock")]
    public async Task<IActionResult> Unlock(string userId)
    {
        var user = await FindStaffAsync(userId);
        if (user is null) return NotFound(new { message = "Staff member not found." });

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new { message = "Account unlocked." });
    }

    // Blocks sign-in and ends existing sessions (checked on every request).
    [HttpPost("staff/{userId}/deactivate")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return BadRequest(new { message = "You cannot deactivate your own account." });

        var user = await FindStaffAsync(userId);
        if (user is null) return NotFound(new { message = "Staff member not found." });

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _userManager.UpdateSecurityStampAsync(user);

        return Ok(new { message = "Account deactivated. Reassign their estates to another controller." });
    }

    [HttpPost("staff/{userId}/reactivate")]
    public async Task<IActionResult> Reactivate(string userId)
    {
        var user = await FindStaffAsync(userId);
        if (user is null) return NotFound(new { message = "Staff member not found." });

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new { message = "Account reactivated." });
    }

    // For a lost or replaced phone: the user sets up 2FA again after signing in.
    [HttpPost("staff/{userId}/reset-2fa")]
    public async Task<IActionResult> ResetTwoFactor(string userId)
    {
        var user = await FindStaffAsync(userId);
        if (user is null) return NotFound(new { message = "Staff member not found." });

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        return Ok(new { message = "Two-factor authentication reset. The user can set it up again from their profile." });
    }

    // ======================================================
    // ESTATE ASSIGNMENT
    // ======================================================

    [HttpGet("estates")]
    public async Task<ActionResult<List<AdminEstateDto>>> GetEstates()
    {
        var estates = await _db.Estates
            .AsNoTracking()
            .Include(e => e.Deceased)
            .Include(e => e.Controller)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .OrderBy(e => e.FileNumber)
            .ToListAsync();

        return Ok(estates.Select(ToAdminDto).ToList());
    }

    [HttpPut("estates/{targetEstateId:guid}/controller")]
    public async Task<IActionResult> Reassign(Guid targetEstateId, [FromBody] ReassignEstateRequest request)
    {
        var estate = await _db.Estates.FirstOrDefaultAsync(e => e.EstateId == targetEstateId);
        if (estate is null) return NotFound(new { message = "Estate not found." });

        var controller = await _db.Controllers.FirstOrDefaultAsync(c => c.ControllerId == request.ControllerId);
        if (controller is null) return BadRequest(new { message = "Choose a staff member to assign the estate to." });

        estate.ControllerId = controller.ControllerId;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Estate {estate.FileNumber} is now assigned to {controller.StaffName}." });
    }

    // ======================================================
    // AUDIT LOG (POPIA)
    // ======================================================

    [HttpGet("audit")]
    public async Task<ActionResult<List<AuditEntryDto>>> GetAudit(
        [FromQuery] Guid? estate,
        [FromQuery] string? user,
        [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 1000);

        var query = _db.AuditEntries.AsNoTracking().AsQueryable();

        if (estate is Guid estateId)
            query = query.Where(a => a.EstateId == estateId);

        if (!string.IsNullOrWhiteSpace(user))
            query = query.Where(a => a.UserName.Contains(user));

        var entries = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(take)
            .ToListAsync();

        var estateIds = entries.Where(e => e.EstateId.HasValue).Select(e => e.EstateId!.Value).Distinct().ToList();
        var fileNumbers = await _db.Estates
            .Where(e => estateIds.Contains(e.EstateId))
            .ToDictionaryAsync(e => e.EstateId, e => e.FileNumber);

        return Ok(entries.Select(a => new AuditEntryDto
        {
            TimestampUtc = a.TimestampUtc,
            UserName = a.UserName,
            Action = a.Action,
            EstateId = a.EstateId,
            FileNumber = a.EstateId is Guid id ? fileNumbers.GetValueOrDefault(id) : null,
            Target = a.Target,
            StatusCode = a.StatusCode
        }).ToList());
    }

    // ======================================================
    // LETTER TEMPLATES
    // ======================================================

    [HttpGet("letter-templates")]
    public async Task<ActionResult<List<LetterTemplateDto>>> GetLetterTemplates() =>
        Ok(await _templates.ListAsync());

    [HttpPut("letter-templates/{letterType}")]
    public async Task<ActionResult<LetterTemplateDto>> SaveLetterTemplate(
        string letterType,
        [FromBody] UpdateLetterTemplateRequest request)
    {
        try
        {
            return Ok(await _templates.SaveAsync(letterType, request, User.FindFirstValue(ClaimTypes.Name) ?? "admin"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotSupportedException)
        {
            return NotFound(new { message = "Unknown letter type." });
        }
    }

    [HttpDelete("letter-templates/{letterType}")]
    public async Task<ActionResult<LetterTemplateDto>> ResetLetterTemplate(string letterType)
    {
        try
        {
            return Ok(await _templates.ResetAsync(letterType));
        }
        catch (NotSupportedException)
        {
            return NotFound(new { message = "Unknown letter type." });
        }
    }

    // ======================================================
    // RECORD RETENTION (POPIA s14)
    // Estates finalised longer ago than the agreed retention period.
    // Nothing is deleted automatically; an administrator reviews and purges.
    // ======================================================

    [HttpGet("retention")]
    public async Task<ActionResult<List<AdminEstateDto>>> GetRetentionCandidates()
    {
        var cutoff = RetentionCutoff();

        var estates = await _db.Estates
            .AsNoTracking()
            .Include(e => e.Deceased)
            .Include(e => e.Controller)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .Where(e => e.Milestones.Any(m => m.Stage == EstateMilestoneStage.Finalised && m.CompletedOn < cutoff))
            .ToListAsync();

        return Ok(estates.Select(ToAdminDto).OrderBy(e => e.FinalisedOn).ToList());
    }

    [HttpDelete("estates/{targetEstateId:guid}")]
    public async Task<IActionResult> PurgeEstate(Guid targetEstateId)
    {
        var estate = await _db.Estates
            .Include(e => e.Milestones)
            .Include(e => e.ScannedDocuments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.EstateId == targetEstateId);

        if (estate is null) return NotFound(new { message = "Estate not found." });

        var finalised = estate.Milestones.FirstOrDefault(m => m.Stage == EstateMilestoneStage.Finalised);
        if (finalised is null || finalised.CompletedOn >= RetentionCutoff())
        {
            return Conflict(new
            {
                message = $"Only estates finalised more than {RetentionYears()} years ago can be purged."
            });
        }

        foreach (var document in estate.ScannedDocuments)
        {
            try
            {
                await _blobStorage.DeleteAsync(document.BlobReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete blob for document {DocumentId} during purge.", document.DocumentId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "The estate's documents could not be deleted from storage, so nothing was purged. Please try again."
                });
            }
        }

        _db.Estates.Remove(estate);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Estate {estate.FileNumber} and its documents were permanently deleted." });
    }

    private int RetentionYears() =>
        int.TryParse(_configuration["Retention:FinalisedEstateYears"], out var years) && years > 0 ? years : 5;

    private DateTime RetentionCutoff() =>
        DateTime.UtcNow.Date.AddYears(-RetentionYears());

    private async Task<ApplicationUser?> FindStaffAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => StaffRoles.Contains(r)) ? user : null;
    }

    public static bool IsDeactivated(ApplicationUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value.Year >= 9999;

    private static AdminEstateDto ToAdminDto(Estate e) => new()
    {
        EstateId = e.EstateId,
        FileNumber = e.FileNumber,
        DeceasedFullName = e.Deceased?.FullName,
        EstateStatus = EstateStatuses.Normalize(e.EstateStatus),
        ControllerId = e.ControllerId,
        ControllerName = e.Controller?.StaffName,
        FinalisedOn = e.Milestones.FirstOrDefault(m => m.Stage == EstateMilestoneStage.Finalised)?.CompletedOn
    };
}

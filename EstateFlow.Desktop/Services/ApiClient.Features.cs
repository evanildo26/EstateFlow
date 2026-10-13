using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Services;

// Liabilities, the stage checklist, correspondence, the portal, 2FA and
// the Firm Administrator screens.
public partial class ApiClient
{
    // ─────────────────────────────────────────────
    // Liabilities, stage checklist, correspondence
    // ─────────────────────────────────────────────

    public async Task AddLiabilityAsync(Guid estateId, CreateLiabilityRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/estates/{estateId}/liabilities", request);
        await EnsureSuccessWithApiMessageAsync(response, "The liability could not be saved");
    }

    public async Task DeleteLiabilityAsync(Guid estateId, Guid liabilityId)
    {
        var response = await _httpClient.DeleteAsync($"api/estates/{estateId}/liabilities/{liabilityId}");
        await EnsureSuccessWithApiMessageAsync(response, "The liability could not be removed");
    }

    public async Task<EstateProgressDto> CompleteMilestoneAsync(Guid estateId, string stage, CompleteMilestoneRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/estates/{estateId}/milestones/{stage}", request);
        await EnsureSuccessWithApiMessageAsync(response, "The stage could not be updated");
        return (await response.Content.ReadFromJsonAsync<EstateProgressDto>())!;
    }

    public async Task<EstateProgressDto> ReopenMilestoneAsync(Guid estateId, string stage)
    {
        var response = await _httpClient.DeleteAsync($"api/estates/{estateId}/milestones/{stage}");
        await EnsureSuccessWithApiMessageAsync(response, "The stage could not be reopened");
        return (await response.Content.ReadFromJsonAsync<EstateProgressDto>())!;
    }

    public async Task<List<CorrespondenceDto>> GetCorrespondenceAsync(Guid estateId)
    {
        var response = await _httpClient.GetAsync($"api/estates/{estateId}/correspondence");
        await EnsureSuccessWithApiMessageAsync(response, "The correspondence history could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<CorrespondenceDto>>() ?? new();
    }

    public async Task InviteToPortalAsync(Guid estateId, InvitePortalUserRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/estates/{estateId}/portal-invites", request);
        await EnsureSuccessWithApiMessageAsync(response, "The invitation could not be sent");
    }

    // ─────────────────────────────────────────────
    // Own account (2FA)
    // ─────────────────────────────────────────────

    public async Task<TwoFactorStatusDto> GetTwoFactorStatusAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/2fa/status");
        await EnsureSuccessWithApiMessageAsync(response, "Two-factor status could not be loaded");
        return (await response.Content.ReadFromJsonAsync<TwoFactorStatusDto>())!;
    }

    public async Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync()
    {
        var response = await _httpClient.PostAsync("api/auth/2fa/setup", null);
        await EnsureSuccessWithApiMessageAsync(response, "Two-factor setup could not be started");
        return (await response.Content.ReadFromJsonAsync<TwoFactorSetupResponse>())!;
    }

    public async Task EnableTwoFactorAsync(string code)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/2fa/enable", new EnableTwoFactorRequest { Code = code });
        await EnsureSuccessWithApiMessageAsync(response, "Two-factor authentication could not be enabled");
    }

    // ─────────────────────────────────────────────
    // Firm Administrator
    // ─────────────────────────────────────────────

    public async Task<List<StaffMemberDto>> GetStaffAsync()
    {
        var response = await _httpClient.GetAsync("api/admin/staff");
        await EnsureSuccessWithApiMessageAsync(response, "Staff could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<StaffMemberDto>>() ?? new();
    }

    public async Task CreateStaffAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        await EnsureSuccessWithApiMessageAsync(response, "The staff account could not be created");
    }

    // action: reset-password | unlock | deactivate | reactivate | reset-2fa
    public async Task<string> AdminStaffActionAsync(string userId, string action, object? body = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/admin/staff/{Uri.EscapeDataString(userId)}/{action}",
            body ?? new { });
        await EnsureSuccessWithApiMessageAsync(response, "The action could not be completed");
        return await ReadMessageAsync(response);
    }

    public async Task<List<AdminEstateDto>> GetAllEstatesAsync()
    {
        var response = await _httpClient.GetAsync("api/admin/estates");
        await EnsureSuccessWithApiMessageAsync(response, "Estates could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<AdminEstateDto>>() ?? new();
    }

    public async Task<string> ReassignEstateAsync(Guid estateId, Guid controllerId)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/admin/estates/{estateId}/controller",
            new ReassignEstateRequest { ControllerId = controllerId });
        await EnsureSuccessWithApiMessageAsync(response, "The estate could not be reassigned");
        return await ReadMessageAsync(response);
    }

    public async Task<List<AuditEntryDto>> GetAuditLogAsync(Guid? estateId, string? user, int take)
    {
        var query = $"api/admin/audit?take={take}";
        if (estateId is Guid id) query += $"&estate={id}";
        if (!string.IsNullOrWhiteSpace(user)) query += $"&user={Uri.EscapeDataString(user)}";

        var response = await _httpClient.GetAsync(query);
        await EnsureSuccessWithApiMessageAsync(response, "The audit log could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<AuditEntryDto>>() ?? new();
    }

    public async Task<List<LetterTemplateDto>> GetLetterTemplatesAsync()
    {
        var response = await _httpClient.GetAsync("api/admin/letter-templates");
        await EnsureSuccessWithApiMessageAsync(response, "Letter templates could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<LetterTemplateDto>>() ?? new();
    }

    public async Task<LetterTemplateDto> SaveLetterTemplateAsync(string letterType, UpdateLetterTemplateRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/letter-templates/{letterType}", request);
        await EnsureSuccessWithApiMessageAsync(response, "The template could not be saved");
        return (await response.Content.ReadFromJsonAsync<LetterTemplateDto>())!;
    }

    public async Task<LetterTemplateDto> ResetLetterTemplateAsync(string letterType)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/letter-templates/{letterType}");
        await EnsureSuccessWithApiMessageAsync(response, "The template could not be reset");
        return (await response.Content.ReadFromJsonAsync<LetterTemplateDto>())!;
    }

    public async Task<List<AdminEstateDto>> GetRetentionCandidatesAsync()
    {
        var response = await _httpClient.GetAsync("api/admin/retention");
        await EnsureSuccessWithApiMessageAsync(response, "The retention review could not be loaded");
        return await response.Content.ReadFromJsonAsync<List<AdminEstateDto>>() ?? new();
    }

    public async Task<string> PurgeEstateAsync(Guid estateId)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/estates/{estateId}");
        await EnsureSuccessWithApiMessageAsync(response, "The estate could not be purged");
        return await ReadMessageAsync(response);
    }

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.TryGetProperty("message", out var message)
                ? message.GetString() ?? "Done."
                : "Done.";
        }
        catch (JsonException)
        {
            return "Done.";
        }
    }
}

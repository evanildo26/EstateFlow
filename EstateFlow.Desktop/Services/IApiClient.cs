using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Services;

public interface IApiClient
{
    // ─────────────────────────────────────────────
    // Authentication
    // ─────────────────────────────────────────────

    Task<LoginResponse> LoginAsync(
        string email,
        string password);

    Task<LoginResponse> VerifyTwoFactorAsync(
        string twoFactorToken,
        string code);

    // ─────────────────────────────────────────────
    // Estates
    // ─────────────────────────────────────────────

    Task<List<EstateSummaryDto>> GetEstatesAsync();

    Task<EstateDto?> GetEstateAsync(
        Guid estateId);

    Task<Guid> CreateEstateAsync(
        CreateEstateRequest request);

    Task UpdateEstateAsync(
        Guid estateId,
        UpdateEstateRequest request);

    Task DeleteEstateAsync(
        Guid estateId);

    // ─────────────────────────────────────────────
    // Progress
    // ─────────────────────────────────────────────

    Task AddProgressStageAsync(
        Guid estateId,
        CreateProgressStageRequest request);

    Task UpdateProgressStageAsync(
        Guid estateId,
        UpdateProgressStageRequest request);

    Task DeleteProgressStageAsync(
        Guid estateId,
        Guid stageId);

    // ─────────────────────────────────────────────
    // Diary
    // ─────────────────────────────────────────────

    Task<List<DiaryEntryWithEstateDto>>
        GetAllDiaryEntriesAsync();

    Task AddDiaryEntryAsync(
        Guid estateId,
        CreateDiaryEntryRequest request);

    Task UpdateDiaryEntryStatusAsync(
        Guid estateId,
        Guid diaryId,
        bool isCompleted);

    Task DeleteDiaryEntryAsync(
        Guid estateId,
        Guid diaryId);

    Task SendDiaryEmailAsync(
        Guid estateId,
        Guid diaryId,
        SendDiaryEmailRequest request);

    // ─────────────────────────────────────────────
    // Reports
    // ─────────────────────────────────────────────

    Task<List<ReportSettingDto>>
        GetReportSettingsAsync();

    Task ToggleReportPauseAsync(
        Guid estateId,
        bool isPaused);

    // ─────────────────────────────────────────────
    // Executors
    // ─────────────────────────────────────────────

    Task AddExecutorAsync(
        Guid estateId,
        CreateExecutorRequest request);

    Task DeleteExecutorAsync(
        Guid estateId,
        Guid executorId);

    // ─────────────────────────────────────────────
    // Beneficiaries
    // ─────────────────────────────────────────────

    Task AddBeneficiaryAsync(
        Guid estateId,
        CreateBeneficiaryRequest request);

    Task DeleteBeneficiaryAsync(
        Guid estateId,
        Guid beneficiaryId);

    // ─────────────────────────────────────────────
    // Movable Assets
    // ─────────────────────────────────────────────

    Task AddMovableAssetAsync(
        Guid estateId,
        CreateMovableAssetRequest request);

    Task DeleteMovableAssetAsync(
        Guid estateId,
        Guid assetId);

    // ─────────────────────────────────────────────
    // Immovable Properties
    // ─────────────────────────────────────────────

    Task AddImmovablePropertyAsync(
        Guid estateId,
        CreateImmovablePropertyRequest request);

    Task DeleteImmovablePropertyAsync(
        Guid estateId,
        Guid assetId);

    // ─────────────────────────────────────────────
    // Financial Claims
    // ─────────────────────────────────────────────

    Task AddFinancialClaimAsync(
        Guid estateId,
        CreateFinancialClaimRequest request);

    Task DeleteFinancialClaimAsync(
        Guid estateId,
        Guid claimId);

    // ─────────────────────────────────────────────
    // Letters
    // ─────────────────────────────────────────────

    Task<GeneratedLetterFile> GenerateVehicleLetterToTempFileAsync(
        Guid estateId,
        Guid assetId);

    Task<GeneratedLetterFile> GenerateInstitutionLetterToTempFileAsync(
        Guid estateId,
        Guid claimId);

    // ─────────────────────────────────────────────
    // Scanned Documents
    // ─────────────────────────────────────────────

    Task UploadDocumentAsync(
        Guid estateId,
        string filePath,
        string documentType);

    Task<string> DownloadDocumentToTempFileAsync(
        Guid estateId,
        Guid documentId,
        string suggestedFileName);

    Task DeleteDocumentAsync(
        Guid estateId,
        Guid documentId);

    // ─────────────────────────────────────────────
    // Liabilities, stage checklist, correspondence
    // ─────────────────────────────────────────────

    Task AddLiabilityAsync(Guid estateId, CreateLiabilityRequest request);

    Task DeleteLiabilityAsync(Guid estateId, Guid liabilityId);

    Task<EstateProgressDto> CompleteMilestoneAsync(Guid estateId, string stage, CompleteMilestoneRequest request);

    Task<EstateProgressDto> ReopenMilestoneAsync(Guid estateId, string stage);

    Task<List<CorrespondenceDto>> GetCorrespondenceAsync(Guid estateId);

    Task InviteToPortalAsync(Guid estateId, InvitePortalUserRequest request);

    // ─────────────────────────────────────────────
    // Own account (2FA)
    // ─────────────────────────────────────────────

    Task<TwoFactorStatusDto> GetTwoFactorStatusAsync();

    Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync();

    Task EnableTwoFactorAsync(string code);

    // ─────────────────────────────────────────────
    // Firm Administrator
    // ─────────────────────────────────────────────

    Task<List<StaffMemberDto>> GetStaffAsync();

    Task CreateStaffAsync(RegisterRequest request);

    Task<string> AdminStaffActionAsync(string userId, string action, object? body = null);

    Task<List<AdminEstateDto>> GetAllEstatesAsync();

    Task<string> ReassignEstateAsync(Guid estateId, Guid controllerId);

    Task<List<AuditEntryDto>> GetAuditLogAsync(Guid? estateId, string? user, int take);

    Task<List<LetterTemplateDto>> GetLetterTemplatesAsync();

    Task<LetterTemplateDto> SaveLetterTemplateAsync(string letterType, UpdateLetterTemplateRequest request);

    Task<LetterTemplateDto> ResetLetterTemplateAsync(string letterType);

    Task<List<AdminEstateDto>> GetRetentionCandidatesAsync();

    Task<string> PurgeEstateAsync(Guid estateId);
}

// A generated letter saved locally, and whether a copy was filed on the estate.
public record GeneratedLetterFile(string Path, bool FiledOnEstate);

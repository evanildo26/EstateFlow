using EstateFlow.Core.Dtos;

namespace EstateFlow.Api.Services;

public interface IEstateService
{
    Task<EstateDto?> GetEstateAsync(
        Guid estateId);

    Task<List<EstateSummaryDto>>
        GetAllEstatesAsync();

    Task<List<EstateSummaryDto>>
        GetEstatesForControllerAsync(
            Guid controllerId);

    // ─────────────────────────────────────────────
    // Progress
    // ─────────────────────────────────────────────

    Task<Guid> AddProgressStageAsync(
        Guid estateId,
        CreateProgressStageRequest request);

    Task<Guid> UpdateProgressStageAsync(
        Guid estateId,
        UpdateProgressStageRequest request);

    Task DeleteProgressStageAsync(
        Guid estateId,
        Guid stageId);

    // ─────────────────────────────────────────────
    // Diary
    // ─────────────────────────────────────────────

    Task<List<DiaryEntryWithEstateDto>>
        GetAllDiaryEntriesAsync(
            Guid controllerId);

    Task<Guid> AddDiaryEntryAsync(
        Guid estateId,
        CreateDiaryEntryRequest request);

    Task UpdateDiaryEntryStatusAsync(
        Guid estateId,
        Guid diaryId,
        bool isCompleted);

    Task DeleteDiaryEntryAsync(
        Guid estateId,
        Guid diaryId);

    // ─────────────────────────────────────────────
    // Reports
    // ─────────────────────────────────────────────

    Task<List<ReportSettingDto>>
        GetReportSettingsAsync(
            Guid controllerId);

    Task ToggleReportPauseAsync(
        Guid estateId,
        bool isPaused);

    // ─────────────────────────────────────────────
    // Estate
    // ─────────────────────────────────────────────

    Task<Guid> CreateEstateAsync(
        CreateEstateRequest request,
        Guid controllerId);

    Task UpdateEstateAsync(
        Guid estateId,
        UpdateEstateRequest request);
    Task DeleteEstateAsync(Guid estateId);

    // ─────────────────────────────────────────────
    // Executor
    // ─────────────────────────────────────────────

    Task<Guid> AddExecutorAsync(
        Guid estateId,
        CreateExecutorRequest request);

    Task DeleteExecutorAsync(
        Guid estateId,
        Guid executorId);

    // ─────────────────────────────────────────────
    // Beneficiary
    // ─────────────────────────────────────────────

    Task<Guid> AddBeneficiaryAsync(
        Guid estateId,
        CreateBeneficiaryRequest request);

    Task DeleteBeneficiaryAsync(
        Guid estateId,
        Guid beneficiaryId);

    // ─────────────────────────────────────────────
    // Movable Asset
    // ─────────────────────────────────────────────

    Task<Guid> AddMovableAssetAsync(
        Guid estateId,
        CreateMovableAssetRequest request);

    Task DeleteMovableAssetAsync(
        Guid estateId,
        Guid assetId);

    // ─────────────────────────────────────────────
    // Immovable Property
    // ─────────────────────────────────────────────

    Task<Guid> AddImmovablePropertyAsync(
        Guid estateId,
        CreateImmovablePropertyRequest request);

    Task DeleteImmovablePropertyAsync(
        Guid estateId,
        Guid assetId);

    // ─────────────────────────────────────────────
    // Financial Claim
    // ─────────────────────────────────────────────

    Task<Guid> AddFinancialClaimAsync(
        Guid estateId,
        CreateFinancialClaimRequest request);

    Task DeleteFinancialClaimAsync(
        Guid estateId,
        Guid claimId);
}
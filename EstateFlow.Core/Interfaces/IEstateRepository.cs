using EstateFlow.Core.Models;

namespace EstateFlow.Core.Interfaces;

public interface IEstateRepository
{
    Task<Estate?> GetByIdAsync(Guid estateId);

    Task<List<Estate>> GetAllAsync();

    Task<List<Estate>> GetByControllerIdAsync(
        Guid controllerId);

    Task<Estate> AddAsync(
        Estate estate);

    Task SaveAsync(
        Estate estate);

    Task DeleteAsync(
        Guid estateId);

    Task<bool> ExistsAsync(
        Guid estateId);

    // Explicit child inserts
    Task AddExecutorAsync(Executor executor);

    Task AddBeneficiaryAsync(
        Beneficiary beneficiary);

    Task AddMovableAssetAsync(
        MovableAsset asset);

    Task AddImmovablePropertyAsync(
        ImmovableProperty property);

    Task AddFinancialClaimAsync(
        FinancialClaim claim);

    Task AddDiaryEntryAsync(
        DiaryEntry diaryEntry);

    Task AddProgressStageAsync(
        ProgressStage progressStage);
}
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Services;

public interface IApiClient
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<LoginResponse> VerifyTwoFactorAsync(string email, string code);
    Task<List<EstateSummaryDto>> GetEstatesAsync();
    Task<EstateDto?> GetEstateAsync(Guid estateId);
    Task UpdateProgressStageAsync(Guid estateId, UpdateProgressStageRequest request);
    Task<List<DiaryEntryWithEstateDto>> GetAllDiaryEntriesAsync();
    Task<List<ReportSettingDto>> GetReportSettingsAsync();
    Task ToggleReportPauseAsync(Guid estateId, bool isPaused);
    Task<Guid> CreateEstateAsync(CreateEstateRequest request);
    Task UpdateEstateAsync(Guid estateId, UpdateEstateRequest request);
}
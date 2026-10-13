using EstateFlow.Core.Dtos;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EstateFlow.Desktop.Services;

public partial class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ─────────────────────────────────────────────
    // Authentication
    // ─────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(
        string email,
        string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");

        return (await response.Content
            .ReadFromJsonAsync<LoginResponse>())!;
    }

    public async Task<LoginResponse> VerifyTwoFactorAsync(
        string twoFactorToken,
        string code)
    {
        var request = new VerifyTwoFactorRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = code
        };

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/auth/login/verify-2fa",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");

        return (await response.Content
            .ReadFromJsonAsync<LoginResponse>())!;
    }

    // ─────────────────────────────────────────────
    // Estates
    // ─────────────────────────────────────────────

    public async Task<List<EstateSummaryDto>>
        GetEstatesAsync()
    {
        var result =
            await _httpClient
                .GetFromJsonAsync<List<EstateSummaryDto>>(
                    "api/estates");

        return result ??
               new List<EstateSummaryDto>();
    }

    public async Task<EstateDto?> GetEstateAsync(
        Guid estateId)
    {
        var response =
            await _httpClient.GetAsync(
                $"api/estates/{estateId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<EstateDto>();
    }

    public async Task<Guid> CreateEstateAsync(
        CreateEstateRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "api/estates",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");

        var result =
            await response.Content
                .ReadFromJsonAsync<Dictionary<string, Guid>>();

        return result!["estateId"];
    }

    public async Task UpdateEstateAsync(
        Guid estateId,
        UpdateEstateRequest request)
    {
        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/estates/{estateId}",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteEstateAsync(
        Guid estateId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Progress
    // ─────────────────────────────────────────────

    public async Task AddProgressStageAsync(
        Guid estateId,
        CreateProgressStageRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/progress-stages",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task UpdateProgressStageAsync(
        Guid estateId,
        UpdateProgressStageRequest request)
    {
        var response =
            await _httpClient.PatchAsJsonAsync(
                $"api/estates/{estateId}/progress-stage",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteProgressStageAsync(
        Guid estateId,
        Guid stageId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/progress-stages/{stageId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Diary
    // ─────────────────────────────────────────────

    public async Task<List<DiaryEntryWithEstateDto>>
        GetAllDiaryEntriesAsync()
    {
        var result =
            await _httpClient
                .GetFromJsonAsync<List<DiaryEntryWithEstateDto>>(
                    "api/diary");

        return result ??
               new List<DiaryEntryWithEstateDto>();
    }

    public async Task AddDiaryEntryAsync(
        Guid estateId,
        CreateDiaryEntryRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/diary/estate/{estateId}",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task UpdateDiaryEntryStatusAsync(
        Guid estateId,
        Guid diaryId,
        bool isCompleted)
    {
        var request =
            new UpdateDiaryEntryStatusRequest
            {
                IsCompleted = isCompleted
            };

        var response =
            await _httpClient.PatchAsJsonAsync(
                $"api/diary/estate/{estateId}/{diaryId}/status",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteDiaryEntryAsync(
        Guid estateId,
        Guid diaryId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/diary/estate/{estateId}/{diaryId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task SendDiaryEmailAsync(
        Guid estateId,
        Guid diaryId,
        SendDiaryEmailRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/diary/estate/{estateId}/{diaryId}/send-email",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "Diary email could not be sent");
    }

    // ─────────────────────────────────────────────
    // Reports
    // ─────────────────────────────────────────────

    public async Task<List<ReportSettingDto>>
        GetReportSettingsAsync()
    {
        var result =
            await _httpClient
                .GetFromJsonAsync<List<ReportSettingDto>>(
                    "api/reports/settings");

        return result ??
               new List<ReportSettingDto>();
    }

    public async Task ToggleReportPauseAsync(
        Guid estateId,
        bool isPaused)
    {
        var request =
            new ToggleReportPauseRequest
            {
                IsPaused = isPaused
            };

        var response =
            await _httpClient.PatchAsJsonAsync(
                $"api/reports/settings/{estateId}",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Executors
    // ─────────────────────────────────────────────

    public async Task AddExecutorAsync(
        Guid estateId,
        CreateExecutorRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/executors",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteExecutorAsync(
        Guid estateId,
        Guid executorId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/executors/{executorId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Beneficiaries
    // ─────────────────────────────────────────────

    public async Task AddBeneficiaryAsync(
        Guid estateId,
        CreateBeneficiaryRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/beneficiaries",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteBeneficiaryAsync(
        Guid estateId,
        Guid beneficiaryId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/beneficiaries/{beneficiaryId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Movable Assets
    // ─────────────────────────────────────────────

    public async Task AddMovableAssetAsync(
        Guid estateId,
        CreateMovableAssetRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/movable-assets",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteMovableAssetAsync(
        Guid estateId,
        Guid assetId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/movable-assets/{assetId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Immovable Properties
    // ─────────────────────────────────────────────

    public async Task AddImmovablePropertyAsync(
        Guid estateId,
        CreateImmovablePropertyRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/immovable-properties",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteImmovablePropertyAsync(
        Guid estateId,
        Guid assetId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/immovable-properties/{assetId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Financial Claims
    // ─────────────────────────────────────────────

    public async Task AddFinancialClaimAsync(
        Guid estateId,
        CreateFinancialClaimRequest request)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                $"api/estates/{estateId}/financial-claims",
                request);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    public async Task DeleteFinancialClaimAsync(
        Guid estateId,
        Guid claimId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/financial-claims/{claimId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");
    }

    // ─────────────────────────────────────────────
    // Letters
    // ─────────────────────────────────────────────

    public async Task<GeneratedLetterFile> GenerateVehicleLetterToTempFileAsync(
        Guid estateId,
        Guid assetId)
    {
        var response =
            await _httpClient.PostAsync(
                $"api/estates/{estateId}/letters/vehicle/{assetId}",
                null);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");

        return await SaveLetterAsync(
            response,
            $"Vehicle_Letter_{assetId}.docx");
    }

    public async Task<GeneratedLetterFile> GenerateInstitutionLetterToTempFileAsync(
        Guid estateId,
        Guid claimId)
    {
        var response =
            await _httpClient.PostAsync(
                $"api/estates/{estateId}/letters/institution/{claimId}",
                null);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "The request could not be completed");

        return await SaveLetterAsync(
            response,
            $"Institution_Letter_{claimId}.docx");
    }

    // ─────────────────────────────────────────────
    // Scanned Documents
    // ─────────────────────────────────────────────

    public async Task UploadDocumentAsync(
        Guid estateId,
        string filePath,
        string documentType)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "The selected document could not be found.",
                filePath);

        await using var fileStream =
            File.OpenRead(filePath);

        using var form =
            new MultipartFormDataContent();

        using var fileContent =
            new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                GetContentType(filePath));

        form.Add(
            fileContent,
            "file",
            Path.GetFileName(filePath));

        form.Add(
            new StringContent(documentType),
            "documentType");

        var response =
            await _httpClient.PostAsync(
                $"api/estates/{estateId}/documents",
                form);

        await EnsureSuccessWithApiMessageAsync(
            response,
            "Document upload failed");
    }

    public async Task<string> DownloadDocumentToTempFileAsync(
        Guid estateId,
        Guid documentId,
        string suggestedFileName)
    {
        var response =
            await _httpClient.GetAsync(
                $"api/estates/{estateId}/documents/{documentId}/download");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "Document download failed");

        var safeFileName =
            string.IsNullOrWhiteSpace(suggestedFileName)
                ? $"{documentId}.tmp"
                : suggestedFileName;

        var tempPath =
            TempFiles.PathFor(safeFileName);

        await using (var fileStream =
                     File.Create(tempPath))
        {
            await response.Content
                .CopyToAsync(fileStream);
        }

        return tempPath;
    }


    public async Task DeleteDocumentAsync(
        Guid estateId,
        Guid documentId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/estates/{estateId}/documents/{documentId}");

        await EnsureSuccessWithApiMessageAsync(
            response,
            "Document deletion failed");
    }

    private static async Task EnsureSuccessWithApiMessageAsync(
        HttpResponseMessage response,
        string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body =
            await response.Content.ReadAsStringAsync();

        var message = string.Empty;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var json =
                    JsonDocument.Parse(body);

                if (json.RootElement.TryGetProperty(
                        "message",
                        out var messageElement))
                {
                    message =
                        messageElement.GetString()
                        ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                message = body;
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message =
                $"{fallbackMessage}. " +
                $"Server returned {(int)response.StatusCode} ({response.StatusCode}).";
        }

        // Keep the status code: callers (e.g. sign-in) react to 401/423/429.
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static async Task<GeneratedLetterFile> SaveLetterAsync(
        HttpResponseMessage response,
        string fallbackFileName)
    {
        var path =
            await SaveResponseToTempFileAsync(
                response,
                fallbackFileName);

        var filed =
            response.Headers.TryGetValues("X-EstateFlow-Letter-Filed", out var values) &&
            values.FirstOrDefault() == "true";

        return new GeneratedLetterFile(path, filed);
    }

    private static async Task<string> SaveResponseToTempFileAsync(
        HttpResponseMessage response,
        string fallbackFileName)
    {
        var headerFileName =
            response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;

        var fileName =
            string.IsNullOrWhiteSpace(headerFileName)
                ? fallbackFileName
                : headerFileName.Trim('"');

        fileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = fallbackFileName;

        var tempPath =
            TempFiles.PathFor(fileName);

        await using var fileStream =
            File.Create(tempPath);

        await response.Content.CopyToAsync(
            fileStream);

        return tempPath;
    }

    private static string GetContentType(
        string filePath)
    {
        return Path.GetExtension(filePath)
            .ToLowerInvariant() switch
        {
            ".pdf" =>
                "application/pdf",

            ".jpg" or ".jpeg" =>
                "image/jpeg",

            ".png" =>
                "image/png",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            _ =>
                "application/octet-stream"
        };
    }
}
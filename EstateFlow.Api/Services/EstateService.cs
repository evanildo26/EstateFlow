using System.Net.Mail;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Interfaces;
using EstateFlow.Core.Models;

namespace EstateFlow.Api.Services;

public class EstateService : IEstateService
{
    private readonly IEstateRepository _repository;

    public EstateService(
        IEstateRepository repository)
    {
        _repository = repository;
    }

    // =====================================================
    // GET ESTATE
    // =====================================================

    public async Task<EstateDto?> GetEstateAsync(
        Guid estateId)
    {
        var estate =
            await _repository.GetByIdAsync(estateId);

        if (estate is null)
            return null;

        return new EstateDto
        {
            EstateId = estate.EstateId,
            FileNumber = estate.FileNumber,
            MastersReference = estate.MastersReference,
            DateOfDeath = estate.DateOfDeath,
            EstateStatus = estate.EstateStatus,
            IsDutiable = estate.IsDutiable,
            ControllerId = estate.ControllerId,

            Controller =
                estate.Controller is null
                    ? null
                    : new EstateControllerDto
                    {
                        ControllerId = estate.Controller.ControllerId,
                        StaffName = estate.Controller.StaffName,
                        Role = estate.Controller.Role,
                        Email = estate.Controller.Email
                    },

            Deceased =
                estate.Deceased is null
                    ? null
                    : new DeceasedDto
                    {
                        DeceasedId =
                            estate.Deceased.DeceasedId,

                        FullName =
                            estate.Deceased.FullName,

                        IdNumber =
                            estate.Deceased.IdNumber,

                        MaritalStatus =
                            estate.Deceased.MaritalStatus
                    },

            Executors =
                estate.Executors
                    .Select(x => new ExecutorDto
                    {
                        ExecutorId = x.ExecutorId,
                        FullName = x.FullName,
                        IdNumber = x.IdNumber,
                        Role = x.Role,
                        Email = x.Email
                    })
                    .ToList(),

            Beneficiaries =
                estate.Beneficiaries
                    .Select(x => new BeneficiaryDto
                    {
                        BeneficiaryId =
                            x.BeneficiaryId,

                        FullName = x.FullName,
                        IdNumber = x.IdNumber,

                        Relationship =
                            x.Relationship,

                        HeirType = x.HeirType,
                        Email = x.Email
                    })
                    .ToList(),

            MovableAssets =
                estate.MovableAssets
                    .Select(x => new MovableAssetDto
                    {
                        AssetId = x.AssetId,
                        Description = x.Description,
                        AssetType = x.AssetType,
                        Value = x.Value,
                        IsVehicle = x.IsVehicle,

                        RegistrationNumber =
                            x.RegistrationNumber,

                        VinNumber = x.VinNumber,
                        Make = x.Make
                    })
                    .ToList(),

            ImmovableProperties =
                estate.ImmovableProperties
                    .Select(x =>
                        new ImmovablePropertyDto
                        {
                            AssetId = x.AssetId,

                            Description =
                                x.Description,

                            TitleDeedNumber =
                                x.TitleDeedNumber,

                            MunicipalValue =
                                x.MunicipalValue
                        })
                    .ToList(),

            FinancialClaims =
                estate.FinancialClaims
                    .Select(x =>
                        new FinancialClaimDto
                        {
                            ClaimId = x.ClaimId,

                            InstitutionName =
                                x.InstitutionName,

                            InstitutionAddress =
                                x.InstitutionAddress,

                            AccountReference =
                                x.AccountReference,

                            Value = x.Value
                        })
                    .ToList(),

            DiaryEntries =
                estate.DiaryEntries
                    .OrderBy(x => x.IsCompleted)
                    .ThenBy(x => x.DueDate)
                    .Select(x =>
                        new DiaryEntryDto
                        {
                            DiaryId = x.DiaryId,
                            DueDate = x.DueDate,
                            Note = x.Note,

                            IsCompleted =
                                x.IsCompleted,

                            NotifyBy = x.NotifyBy
                        })
                    .ToList(),

            ProgressStages =
                estate.ProgressStages
                    .OrderByDescending(x => x.TargetDate)
                    .ThenByDescending(x => x.StageId)
                    .Select(x =>
                        new ProgressStageDto
                        {
                            StageId = x.StageId,

                            StageName =
                                x.StageName,

                            TargetDate =
                                x.TargetDate,

                            Status = x.Status
                        })
                    .ToList(),

            ScannedDocuments =
                estate.ScannedDocuments
                    .Select(x =>
                        new ScannedDocumentDto
                        {
                            DocumentId =
                                x.DocumentId,

                            DocumentType =
                                x.DocumentType,

                            FileName =
                                x.FileName,

                            UploadDate =
                                x.UploadDate
                        })
                    .ToList()
        };
    }

    // =====================================================
    // GET ESTATES
    // =====================================================

    public async Task<List<EstateSummaryDto>>
        GetAllEstatesAsync()
    {
        var estates =
            await _repository.GetAllAsync();

        return estates
            .Select(MapToSummary)
            .ToList();
    }

    public async Task<List<EstateSummaryDto>>
        GetEstatesForControllerAsync(
            Guid controllerId)
    {
        var estates =
            await _repository
                .GetByControllerIdAsync(
                    controllerId);

        return estates
            .Select(MapToSummary)
            .ToList();
    }

    // =====================================================
    // PROGRESS
    // =====================================================

    public async Task<Guid> AddProgressStageAsync(
        Guid estateId,
        CreateProgressStageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StageName))
        {
            throw new ArgumentException(
                "Progress update is required.");
        }

        var validStatuses =
            new[]
            {
                "Pending",
                "InProgress",
                "Complete"
            };

        var normalizedStatus =
            validStatuses.FirstOrDefault(
                status => string.Equals(
                    status,
                    request.Status,
                    StringComparison.OrdinalIgnoreCase))
            ?? "InProgress";

        var estate =
            await _repository.GetByIdAsync(estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var stage =
            new ProgressStage
            {
                StageId = Guid.NewGuid(),
                EstateId = estateId,
                StageName = request.StageName.Trim(),
                TargetDate = request.TargetDate ?? DateTime.Today,
                Status = normalizedStatus
            };

        await _repository.AddProgressStageAsync(stage);

        return stage.StageId;
    }

    public async Task<Guid> UpdateProgressStageAsync(
        Guid estateId,
        UpdateProgressStageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StageName))
        {
            throw new ArgumentException(
                "Progress update is required.");
        }

        var validStatuses =
            new[]
            {
                "Pending",
                "InProgress",
                "Complete"
            };

        var normalizedStatus =
            validStatuses.FirstOrDefault(
                status => string.Equals(
                    status,
                    request.Status,
                    StringComparison.OrdinalIgnoreCase));

        if (normalizedStatus is null)
        {
            throw new ArgumentException(
                "Progress status must be Pending, InProgress or Complete.");
        }

        var estate =
            await _repository.GetByIdAsync(estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var stage =
            estate.ProgressStages
                .FirstOrDefault(
                    x => x.StageId == request.StageId)
            ?? throw new InvalidOperationException(
                $"Progress update {request.StageId} not found on estate {estateId}.");

        stage.StageName = request.StageName.Trim();
        stage.TargetDate = request.TargetDate ?? DateTime.Today;
        stage.Status = normalizedStatus;

        await _repository.SaveAsync(estate);

        return stage.StageId;
    }

    public async Task DeleteProgressStageAsync(
        Guid estateId,
        Guid stageId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var stage =
            estate.ProgressStages
                .FirstOrDefault(
                    x => x.StageId == stageId)
            ?? throw new InvalidOperationException(
                $"Progress stage {stageId} not found.");

        estate.ProgressStages.Remove(stage);

        await _repository.SaveAsync(estate);
    }

    // =====================================================
    // DIARY
    // =====================================================

    public async Task<List<DiaryEntryWithEstateDto>>
        GetAllDiaryEntriesAsync(
            Guid controllerId)
    {
        var estates =
            await _repository
                .GetByControllerIdAsync(controllerId);

        return estates
            .SelectMany(
                e => e.DiaryEntries.Select(
                    d => new DiaryEntryWithEstateDto
                    {
                        DiaryId = d.DiaryId,
                        EstateId = e.EstateId,
                        FileNumber = e.FileNumber,

                        DeceasedFullName =
                            e.Deceased?.FullName,

                        Note = d.Note,
                        DueDate = d.DueDate,

                        IsCompleted =
                            d.IsCompleted,

                        NotifyBy = d.NotifyBy
                    }))
            .OrderBy(d => d.IsCompleted)
            .ThenBy(d => d.DueDate)
            .ToList();
    }

    public async Task<Guid> AddDiaryEntryAsync(
        Guid estateId,
        CreateDiaryEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.Note))
        {
            throw new ArgumentException(
                "Diary note is required.");
        }

        if (request.DueDate == default)
        {
            throw new ArgumentException(
                "Diary due date is required.");
        }

        var validNotificationMethods =
            new[]
            {
                "Email",
                "InApp",
                "Both"
            };

        var notifyBy =
            validNotificationMethods
                .FirstOrDefault(
                    x => string.Equals(
                        x,
                        request.NotifyBy,
                        StringComparison.OrdinalIgnoreCase));

        if (notifyBy is null)
        {
            throw new ArgumentException(
                "Notification method must be Email, InApp or Both.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var diaryEntry =
            new DiaryEntry
            {
                DiaryId = Guid.NewGuid(),
                EstateId = estateId,
                Note = request.Note.Trim(),
                DueDate = request.DueDate,
                NotifyBy = notifyBy,
                IsCompleted = false
            };

        await _repository.AddDiaryEntryAsync(
            diaryEntry);

        return diaryEntry.DiaryId;
    }

    public async Task UpdateDiaryEntryStatusAsync(
        Guid estateId,
        Guid diaryId,
        bool isCompleted)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var diaryEntry =
            estate.DiaryEntries
                .FirstOrDefault(
                    x => x.DiaryId == diaryId)
            ?? throw new InvalidOperationException(
                $"Diary entry {diaryId} not found.");

        diaryEntry.IsCompleted =
            isCompleted;

        await _repository.SaveAsync(estate);
    }

    public async Task DeleteDiaryEntryAsync(
        Guid estateId,
        Guid diaryId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var diaryEntry =
            estate.DiaryEntries
                .FirstOrDefault(
                    x => x.DiaryId == diaryId)
            ?? throw new InvalidOperationException(
                $"Diary entry {diaryId} not found.");

        estate.DiaryEntries.Remove(
            diaryEntry);

        await _repository.SaveAsync(estate);
    }

    // =====================================================
    // REPORTS
    // =====================================================

    public async Task<List<ReportSettingDto>>
        GetReportSettingsAsync(
            Guid controllerId)
    {
        var estates =
            await _repository
                .GetByControllerIdAsync(controllerId);

        return estates.Select(e =>
        {
            var latestReport =
                e.AutomatedReports
                    .OrderByDescending(
                        r => r.ReportPeriod)
                    .FirstOrDefault();

            return new ReportSettingDto
            {
                EstateId = e.EstateId,
                FileNumber = e.FileNumber,

                DeceasedFullName =
                    e.Deceased?.FullName,

                LastSentDate =
                    latestReport?.SentDate,

                IsPaused =
                    latestReport?.IsPaused
                    ?? false
            };
        }).ToList();
    }

    public async Task ToggleReportPauseAsync(
        Guid estateId,
        bool isPaused)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var latestReport =
            estate.AutomatedReports
                .OrderByDescending(
                    r => r.ReportPeriod)
                .FirstOrDefault();

        if (latestReport is not null)
        {
            latestReport.IsPaused =
                isPaused;
        }
        else
        {
            estate.AutomatedReports.Add(
                new AutomatedReport
                {
                    EstateId = estateId,

                    ReportPeriod =
                        DateTime.UtcNow,

                    RecipientType =
                        "Executor",

                    SentDate = null,
                    IsPaused = isPaused
                });
        }

        await _repository.SaveAsync(estate);
    }

    // =====================================================
    // ESTATE
    // =====================================================

    public async Task<Guid> CreateEstateAsync(
        CreateEstateRequest request,
        Guid controllerId)
    {
        if (string.IsNullOrWhiteSpace(
                request.FileNumber))
        {
            throw new ArgumentException(
                "File number is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.DeceasedFullName))
        {
            throw new ArgumentException(
                "Deceased full name is required.");
        }

        var estate =
            new Estate
            {
                FileNumber =
                    request.FileNumber,

                MastersReference =
                    request.MastersReference,

                DateOfDeath =
                    request.DateOfDeath,

                IsDutiable =
                    request.IsDutiable,

                EstateStatus =
                    "Reported",

                ControllerId =
                    controllerId,

                Deceased =
                    new Deceased
                    {
                        FullName =
                            request.DeceasedFullName,

                        IdNumber =
                            request.DeceasedIdNumber,

                        MaritalStatus =
                            request.DeceasedMaritalStatus
                    }
            };

        var created =
            await _repository.AddAsync(
                estate);

        return created.EstateId;
    }

    public async Task UpdateEstateAsync(
        Guid estateId,
        UpdateEstateRequest request)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        estate.FileNumber =
            request.FileNumber;

        estate.MastersReference =
            request.MastersReference;

        estate.DateOfDeath =
            request.DateOfDeath;

        estate.IsDutiable =
            request.IsDutiable;

        estate.EstateStatus =
            request.EstateStatus;

        if (estate.Deceased is null)
        {
            estate.Deceased =
                new Deceased
                {
                    EstateId = estateId,

                    FullName =
                        request.DeceasedFullName,

                    IdNumber =
                        request.DeceasedIdNumber,

                    MaritalStatus =
                        request.DeceasedMaritalStatus
                };
        }
        else
        {
            estate.Deceased.FullName =
                request.DeceasedFullName;

            estate.Deceased.IdNumber =
                request.DeceasedIdNumber;

            estate.Deceased.MaritalStatus =
                request.DeceasedMaritalStatus;
        }

        await _repository.SaveAsync(estate);
    }

    public async Task DeleteEstateAsync(
        Guid estateId)
    {
        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        await _repository.DeleteAsync(
            estateId);
    }

    // =====================================================
    // EXECUTORS
    // =====================================================

    public async Task<Guid> AddExecutorAsync(
        Guid estateId,
        CreateExecutorRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.FullName))
        {
            throw new ArgumentException(
                "Executor full name is required.");
        }

        if (!IsValidEmail(request.Email))
        {
            throw new ArgumentException(
                "A valid executor email address is required.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var executor =
            new Executor
            {
                ExecutorId = Guid.NewGuid(),
                EstateId = estateId,

                FullName =
                    request.FullName.Trim(),

                IdNumber =
                    request.IdNumber?.Trim()
                    ?? string.Empty,

                Role =
                    request.Role?.Trim()
                    ?? string.Empty,

                Email =
                    request.Email.Trim()
            };

        await _repository.AddExecutorAsync(
            executor);

        return executor.ExecutorId;
    }

    public async Task DeleteExecutorAsync(
        Guid estateId,
        Guid executorId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var executor =
            estate.Executors
                .FirstOrDefault(
                    x => x.ExecutorId ==
                         executorId)
            ?? throw new InvalidOperationException(
                $"Executor {executorId} not found.");

        estate.Executors.Remove(
            executor);

        await _repository.SaveAsync(
            estate);
    }

    // =====================================================
    // BENEFICIARIES
    // =====================================================

    public async Task<Guid> AddBeneficiaryAsync(
        Guid estateId,
        CreateBeneficiaryRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.FullName))
        {
            throw new ArgumentException(
                "Beneficiary full name is required.");
        }

        if (!IsValidEmail(request.Email))
        {
            throw new ArgumentException(
                "A valid beneficiary email address is required.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var beneficiary =
            new Beneficiary
            {
                BeneficiaryId =
                    Guid.NewGuid(),

                EstateId = estateId,

                FullName =
                    request.FullName.Trim(),

                IdNumber =
                    request.IdNumber?.Trim()
                    ?? string.Empty,

                Relationship =
                    request.Relationship?.Trim()
                    ?? string.Empty,

                HeirType =
                    request.HeirType?.Trim()
                    ?? string.Empty,

                Email =
                    request.Email.Trim()
            };

        await _repository
            .AddBeneficiaryAsync(
                beneficiary);

        return beneficiary.BeneficiaryId;
    }

    public async Task DeleteBeneficiaryAsync(
        Guid estateId,
        Guid beneficiaryId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var beneficiary =
            estate.Beneficiaries
                .FirstOrDefault(
                    x => x.BeneficiaryId ==
                         beneficiaryId)
            ?? throw new InvalidOperationException(
                $"Beneficiary {beneficiaryId} not found.");

        estate.Beneficiaries.Remove(
            beneficiary);

        await _repository.SaveAsync(
            estate);
    }

    // =====================================================
    // MOVABLE ASSETS
    // =====================================================

    public async Task<Guid> AddMovableAssetAsync(
        Guid estateId,
        CreateMovableAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.Description))
        {
            throw new ArgumentException(
                "Asset description is required.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var asset =
            new MovableAsset
            {
                AssetId = Guid.NewGuid(),
                EstateId = estateId,

                Description =
                    request.Description,

                AssetType =
                    request.AssetType,

                Value = request.Value,

                IsVehicle =
                    request.IsVehicle,

                RegistrationNumber =
                    request.RegistrationNumber,

                VinNumber =
                    request.VinNumber,

                Make =
                    request.Make
            };

        await _repository.AddMovableAssetAsync(
            asset);

        return asset.AssetId;
    }

    public async Task DeleteMovableAssetAsync(
        Guid estateId,
        Guid assetId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var asset =
            estate.MovableAssets
                .FirstOrDefault(
                    x => x.AssetId == assetId)
            ?? throw new InvalidOperationException(
                $"Movable asset {assetId} not found.");

        estate.MovableAssets.Remove(
            asset);

        await _repository.SaveAsync(
            estate);
    }

    // =====================================================
    // IMMOVABLE PROPERTIES
    // =====================================================

    public async Task<Guid>
        AddImmovablePropertyAsync(
            Guid estateId,
            CreateImmovablePropertyRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.Description))
        {
            throw new ArgumentException(
                "Property description is required.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var property =
            new ImmovableProperty
            {
                AssetId = Guid.NewGuid(),
                EstateId = estateId,

                Description =
                    request.Description,

                TitleDeedNumber =
                    request.TitleDeedNumber,

                MunicipalValue =
                    request.MunicipalValue
            };

        await _repository
            .AddImmovablePropertyAsync(
                property);

        return property.AssetId;
    }

    public async Task DeleteImmovablePropertyAsync(
        Guid estateId,
        Guid assetId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var property =
            estate.ImmovableProperties
                .FirstOrDefault(
                    x => x.AssetId == assetId)
            ?? throw new InvalidOperationException(
                $"Immovable property {assetId} not found.");

        estate.ImmovableProperties.Remove(
            property);

        await _repository.SaveAsync(
            estate);
    }

    // =====================================================
    // FINANCIAL CLAIMS
    // =====================================================

    public async Task<Guid>
        AddFinancialClaimAsync(
            Guid estateId,
            CreateFinancialClaimRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.InstitutionName))
        {
            throw new ArgumentException(
                "Institution name is required.");
        }

        if (!await _repository.ExistsAsync(
                estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var claim =
            new FinancialClaim
            {
                ClaimId = Guid.NewGuid(),
                EstateId = estateId,

                InstitutionName =
                    request.InstitutionName,

                InstitutionAddress =
                    request.InstitutionAddress,

                AccountReference =
                    request.AccountReference,

                Value = request.Value
            };

        await _repository
            .AddFinancialClaimAsync(
                claim);

        return claim.ClaimId;
    }

    public async Task DeleteFinancialClaimAsync(
        Guid estateId,
        Guid claimId)
    {
        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var claim =
            estate.FinancialClaims
                .FirstOrDefault(
                    x => x.ClaimId == claimId)
            ?? throw new InvalidOperationException(
                $"Financial claim {claimId} not found.");

        estate.FinancialClaims.Remove(
            claim);

        await _repository.SaveAsync(
            estate);
    }

    // =====================================================
    // MAPPING
    // =====================================================

    private static EstateSummaryDto MapToSummary(
        Estate estate)
    {
        return new EstateSummaryDto
        {
            EstateId = estate.EstateId,
            FileNumber = estate.FileNumber,

            DeceasedFullName =
                estate.Deceased?.FullName,

            EstateStatus =
                estate.EstateStatus,

            DateOfDeath =
                estate.DateOfDeath,

            ControllerName =
                estate.Controller?.StaffName,

            OpenDiaryCount =
                estate.DiaryEntries.Count(
                    d => !d.IsCompleted)
        };
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private static bool IsValidEmail(
        string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var address =
                new MailAddress(email.Trim());

            return string.Equals(
                address.Address,
                email.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
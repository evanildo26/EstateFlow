using System.Net.Mail;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Interfaces;
using EstateFlow.Core.Models;
using EstateFlow.Core.Validation;
using EstateFlow.Core.Workflow;
using Microsoft.Extensions.Options;

namespace EstateFlow.Api.Services;

public class EstateService : IEstateService
{
    private readonly IEstateRepository _repository;
    private readonly MasterProcessOptions _processOptions;
    private readonly TimeProvider _clock;

    public EstateService(
        IEstateRepository repository,
        IOptions<MasterProcessOptions> processOptions,
        TimeProvider clock)
    {
        _repository = repository;
        _processOptions = processOptions.Value;
        _clock = clock;
    }

    private DateTime Today =>
        _clock.GetLocalNow().Date;

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
            EstateStatus = EstateStatuses.Normalize(estate.EstateStatus),
            IsDutiable = estate.IsDutiable,
            ControllerId = estate.ControllerId,

            Progress = BuildProgress(estate),

            TotalAssets =
                estate.MovableAssets.Sum(x => x.Value) +
                estate.ImmovableProperties.Sum(x => x.MunicipalValue) +
                estate.FinancialClaims.Sum(x => x.Value),

            TotalLiabilities =
                estate.Liabilities.Sum(x => x.Amount),

            Liabilities =
                estate.Liabilities
                    .OrderBy(x => x.CreditorName)
                    .Select(x => new LiabilityDto
                    {
                        LiabilityId = x.LiabilityId,
                        CreditorName = x.CreditorName,
                        Description = x.Description,
                        Amount = x.Amount
                    })
                    .ToList(),

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

                            NotifyBy = x.NotifyBy.ToString()
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

                            Status = x.Status.ToString()
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

        var normalizedStatus =
            StatusParsing.TryParseProgressStatus(request.Status, out var parsedStatus)
                ? parsedStatus
                : ProgressStatus.InProgress;

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
                TargetDate = request.TargetDate ?? Today,
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

        if (!StatusParsing.TryParseProgressStatus(request.Status, out var normalizedStatus))
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
        stage.TargetDate = request.TargetDate ?? Today;
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

                        NotifyBy = d.NotifyBy.ToString()
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

        if (!StatusParsing.TryParseNotifyMethod(request.NotifyBy, out var notifyBy))
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

            await _repository.SaveAsync(estate);
        }
        else
        {
            // Added explicitly: through the navigation, EF would treat the
            // pre-set Guid key as an existing row and the save would fail.
            await _repository.AddAutomatedReportAsync(
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

        var deceasedId =
            ValidateDeceasedIdNumber(
                request.DeceasedIdNumber,
                request.DateOfDeath);

        var estate =
            new Estate
            {
                FileNumber =
                    request.FileNumber.Trim(),

                MastersReference =
                    request.MastersReference,

                DateOfDeath =
                    request.DateOfDeath,

                IsDutiable =
                    request.IsDutiable,

                EstateStatus =
                    EstateStatuses.Reported,

                ControllerId =
                    controllerId,

                Deceased =
                    new Deceased
                    {
                        FullName =
                            request.DeceasedFullName.Trim(),

                        IdNumber =
                            deceasedId,

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
        if (string.IsNullOrWhiteSpace(request.FileNumber))
        {
            throw new ArgumentException(
                "File number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DeceasedFullName))
        {
            throw new ArgumentException(
                "Deceased full name is required.");
        }

        var deceasedId =
            ValidateDeceasedIdNumber(
                request.DeceasedIdNumber,
                request.DateOfDeath);

        var estate =
            await _repository.GetByIdAsync(
                estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        estate.FileNumber =
            request.FileNumber.Trim();

        estate.MastersReference =
            request.MastersReference;

        estate.DateOfDeath =
            request.DateOfDeath;

        estate.IsDutiable =
            request.IsDutiable;

        // Once the stage checklist is in use, the status follows it.
        estate.EstateStatus =
            estate.Milestones.Count > 0
                ? MasterProcess.DeriveEstateStatus(
                    estate.Milestones.Select(m => m.Stage).ToList())
                : EstateStatuses.Normalize(request.EstateStatus);

        if (estate.Deceased is null)
        {
            estate.Deceased =
                new Deceased
                {
                    EstateId = estateId,

                    FullName =
                        request.DeceasedFullName.Trim(),

                    IdNumber =
                        deceasedId,

                    MaritalStatus =
                        request.DeceasedMaritalStatus
                };
        }
        else
        {
            estate.Deceased.FullName =
                request.DeceasedFullName.Trim();

            estate.Deceased.IdNumber =
                deceasedId;

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
                    ValidatePersonIdNumber(
                        request.IdNumber,
                        "executor"),

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
                    ValidatePersonIdNumber(
                        request.IdNumber,
                        "beneficiary"),

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

    private EstateSummaryDto MapToSummary(
        Estate estate)
    {
        var progress = EvaluateProgress(estate);

        var openDiary =
            estate.DiaryEntries
                .Where(d => !d.IsCompleted)
                .ToList();

        return new EstateSummaryDto
        {
            EstateId = estate.EstateId,
            FileNumber = estate.FileNumber,

            DeceasedFullName =
                estate.Deceased?.FullName,

            EstateStatus =
                EstateStatuses.Normalize(estate.EstateStatus),

            DateOfDeath =
                estate.DateOfDeath,

            ControllerName =
                estate.Controller?.StaffName,

            OpenDiaryCount =
                openDiary.Count,

            OverdueDiaryCount =
                openDiary.Count(d => d.DueDate.Date < Today),

            NextDiaryDue =
                openDiary
                    .OrderBy(d => d.DueDate)
                    .Select(d => (DateTime?)d.DueDate)
                    .FirstOrDefault(),

            ProgressPercent =
                progress.PercentComplete,

            NextMilestoneTitle =
                progress.NextMilestone?.Title,

            NextDeadline =
                progress.NextMilestone?.DueDate,

            NeedsAttention =
                progress.NeedsAttention,

            AttentionReason =
                progress.AttentionReason,

            DaysOverdue =
                progress.DaysOverdue
        };
    }

    private EstateProgress EvaluateProgress(Estate estate) =>
        MasterProcess.Evaluate(
            estate.DateOfDeath,
            CompletedMilestones(estate),
            Today,
            _processOptions);

    private static Dictionary<EstateMilestoneStage, DateTime> CompletedMilestones(Estate estate) =>
        estate.Milestones
            .GroupBy(m => m.Stage)
            .ToDictionary(g => g.Key, g => g.First().CompletedOn);

    private EstateProgressDto BuildProgress(Estate estate)
    {
        var completed = CompletedMilestones(estate);
        var progress = EvaluateProgress(estate);

        var notes =
            estate.Milestones
                .GroupBy(m => m.Stage)
                .ToDictionary(g => g.Key, g => g.First().Notes);

        return new EstateProgressDto
        {
            PercentComplete = progress.PercentComplete,
            CompletedCount = progress.CompletedCount,
            TotalCount = MasterProcess.Definitions.Count,
            IsFinalised = progress.IsFinalised,
            NeedsAttention = progress.NeedsAttention,
            AttentionReason = progress.AttentionReason,
            DaysOverdue = progress.DaysOverdue,
            NextMilestoneTitle = progress.NextMilestone?.Title,
            NextDeadline = progress.NextMilestone?.DueDate,
            EarliestAccountLodgingDate =
                MasterProcess.EarliestAccountLodgingDate(completed, _processOptions),

            Milestones =
                progress.Milestones
                    .Select(m => new MilestoneDto
                    {
                        Stage = m.Stage.ToString(),
                        Order = (int)m.Stage,
                        Title = m.Title,
                        Description = m.Description,
                        CompletedOn = m.CompletedOn,
                        Notes = notes.GetValueOrDefault(m.Stage),
                        DueDate = m.DueDate,
                        IsStatutoryDeadline = m.IsStatutoryDeadline,
                        DeadlineBasis = m.DeadlineBasis,
                        IsOverdue = m.IsOverdue,
                        IsDueSoon = m.IsDueSoon,
                        IsNext = m.IsNext
                    })
                    .ToList()
        };
    }

    // =====================================================
    // LIABILITIES (debts of the deceased)
    // =====================================================

    public async Task<Guid> AddLiabilityAsync(
        Guid estateId,
        CreateLiabilityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CreditorName))
        {
            throw new ArgumentException(
                "Creditor name is required.");
        }

        if (request.Amount < 0)
        {
            throw new ArgumentException(
                "The amount owed cannot be negative.");
        }

        if (!await _repository.ExistsAsync(estateId))
        {
            throw new InvalidOperationException(
                $"Estate {estateId} not found.");
        }

        var liability =
            new Liability
            {
                LiabilityId = Guid.NewGuid(),
                EstateId = estateId,
                CreditorName = request.CreditorName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                Amount = request.Amount
            };

        await _repository.AddLiabilityAsync(liability);

        return liability.LiabilityId;
    }

    public async Task DeleteLiabilityAsync(
        Guid estateId,
        Guid liabilityId)
    {
        var estate =
            await _repository.GetByIdAsync(estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var liability =
            estate.Liabilities
                .FirstOrDefault(x => x.LiabilityId == liabilityId)
            ?? throw new InvalidOperationException(
                $"Liability {liabilityId} not found.");

        estate.Liabilities.Remove(liability);

        await _repository.SaveAsync(estate);
    }

    // =====================================================
    // STAGE CHECKLIST (Master's process)
    // =====================================================

    public async Task<EstateProgressDto> CompleteMilestoneAsync(
        Guid estateId,
        string stage,
        CompleteMilestoneRequest request,
        string recordedBy)
    {
        var parsedStage = ParseStage(stage);

        if (request.CompletedOn == default)
        {
            throw new ArgumentException(
                "The date the stage was completed is required.");
        }

        if (request.CompletedOn.Date > Today)
        {
            throw new ArgumentException(
                "A stage cannot be completed in the future.");
        }

        var estate =
            await _repository.GetByIdAsync(estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        if (request.CompletedOn.Date < estate.DateOfDeath.Date)
        {
            throw new ArgumentException(
                "A stage cannot be completed before the date of death.");
        }

        var notes =
            string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim();

        var existing =
            estate.Milestones.FirstOrDefault(m => m.Stage == parsedStage);

        var completedStages =
            estate.Milestones
                .Select(m => m.Stage)
                .Append(parsedStage)
                .Distinct()
                .ToList();

        estate.EstateStatus =
            MasterProcess.DeriveEstateStatus(completedStages);

        if (existing is not null)
        {
            existing.CompletedOn = request.CompletedOn.Date;
            existing.Notes = notes;
            existing.RecordedBy = recordedBy;
            existing.RecordedAtUtc = DateTime.UtcNow;

            await _repository.SaveAsync(estate);
        }
        else
        {
            // Added explicitly: EF would otherwise treat a child with a
            // pre-set Guid key as an existing row and try to update it.
            await _repository.AddMilestoneAsync(
                new EstateMilestone
                {
                    EstateId = estateId,
                    Stage = parsedStage,
                    CompletedOn = request.CompletedOn.Date,
                    Notes = notes,
                    RecordedBy = recordedBy
                });
        }

        return BuildProgress(estate);
    }

    public async Task<EstateProgressDto> ReopenMilestoneAsync(
        Guid estateId,
        string stage)
    {
        var parsedStage = ParseStage(stage);

        var estate =
            await _repository.GetByIdAsync(estateId)
            ?? throw new InvalidOperationException(
                $"Estate {estateId} not found.");

        var existing =
            estate.Milestones.FirstOrDefault(m => m.Stage == parsedStage);

        if (existing is not null)
        {
            estate.Milestones.Remove(existing);

            estate.EstateStatus =
                MasterProcess.DeriveEstateStatus(
                    estate.Milestones.Select(m => m.Stage).ToList());

            await _repository.SaveAsync(estate);
        }

        return BuildProgress(estate);
    }

    private static EstateMilestoneStage ParseStage(string stage)
    {
        if (Enum.TryParse<EstateMilestoneStage>(stage, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ArgumentException(
            $"Unknown stage '{stage}'.");
    }

    // =====================================================
    // ID NUMBERS
    // =====================================================

    // The deceased's ID: must be a valid SA ID (or a passport number), and
    // the date of birth it contains must be before the date of death.
    private static string ValidateDeceasedIdNumber(
        string? idNumber,
        DateTime dateOfDeath)
    {
        var check = SouthAfricanIdNumber.Check(idNumber);

        if (!check.IsAcceptable)
        {
            throw new ArgumentException(
                $"Deceased ID number: {check.Error}");
        }

        if (check.DateOfBirth is DateTime born &&
            dateOfDeath != default &&
            born >= dateOfDeath.Date)
        {
            throw new ArgumentException(
                "The deceased's ID number gives a date of birth on or after the date of death. " +
                "Please check the ID number and the date of death.");
        }

        return check.Normalized;
    }

    private static string ValidatePersonIdNumber(
        string? idNumber,
        string personLabel)
    {
        var check = SouthAfricanIdNumber.Check(idNumber);

        if (!check.IsAcceptable)
        {
            throw new ArgumentException(
                $"The {personLabel}'s ID number is not valid. {check.Error}");
        }

        return check.Normalized;
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
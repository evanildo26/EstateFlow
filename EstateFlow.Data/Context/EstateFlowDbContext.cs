using EstateFlow.Core.Models;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EstateFlow.Data.Context;

public class EstateFlowDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IFieldProtector? _fieldProtector;

    public EstateFlowDbContext(DbContextOptions<EstateFlowDbContext> options)
        : base(options) { }

    // Used when DataProtection:FieldKey is configured: ID numbers and account
    // references are then encrypted at rest.
    public EstateFlowDbContext(
        DbContextOptions<EstateFlowDbContext> options,
        IFieldProtector? fieldProtector)
        : base(options)
    {
        _fieldProtector = fieldProtector;
    }

    // The EF model embeds the encryption converters, so a context with a
    // different key (or none) needs its own cached model.
    internal string FieldProtectorKeyId => _fieldProtector?.KeyId ?? "none";

    public DbSet<Estate> Estates => Set<Estate>();
    public DbSet<EstateController> Controllers => Set<EstateController>();
    public DbSet<Deceased> Deceased => Set<Deceased>();
    public DbSet<Executor> Executors => Set<Executor>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<ImmovableProperty> ImmovableProperties => Set<ImmovableProperty>();
    public DbSet<MovableAsset> MovableAssets => Set<MovableAsset>();
    public DbSet<FinancialClaim> FinancialClaims => Set<FinancialClaim>();
    public DbSet<Liability> Liabilities => Set<Liability>();
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<ProgressStage> ProgressStages => Set<ProgressStage>();
    public DbSet<ScannedDocument> ScannedDocuments => Set<ScannedDocument>();
    public DbSet<AutomatedReport> AutomatedReports => Set<AutomatedReport>();
    public DbSet<EstateMilestone> Milestones => Set<EstateMilestone>();
    public DbSet<CorrespondenceRecord> Correspondence => Set<CorrespondenceRecord>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, FieldProtectorModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Required first — sets up all the Identity tables (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(modelBuilder);

        // Explicit primary keys — several entities use ERD-style property
        // names that don't match EF Core's default "{ClassName}Id" convention.
        modelBuilder.Entity<Estate>().HasKey(e => e.EstateId);
        modelBuilder.Entity<EstateController>().HasKey(e => e.ControllerId);
        modelBuilder.Entity<Deceased>().HasKey(e => e.DeceasedId);
        modelBuilder.Entity<Executor>().HasKey(e => e.ExecutorId);
        modelBuilder.Entity<Beneficiary>().HasKey(e => e.BeneficiaryId);
        modelBuilder.Entity<ImmovableProperty>().HasKey(e => e.AssetId);
        modelBuilder.Entity<MovableAsset>().HasKey(e => e.AssetId);
        modelBuilder.Entity<FinancialClaim>().HasKey(e => e.ClaimId);
        modelBuilder.Entity<Liability>().HasKey(e => e.LiabilityId);
        modelBuilder.Entity<DiaryEntry>().HasKey(e => e.DiaryId);
        modelBuilder.Entity<ProgressStage>().HasKey(e => e.StageId);
        modelBuilder.Entity<ScannedDocument>().HasKey(e => e.DocumentId);
        modelBuilder.Entity<AutomatedReport>().HasKey(e => e.ReportId);
        modelBuilder.Entity<EstateMilestone>().HasKey(e => e.MilestoneId);
        modelBuilder.Entity<CorrespondenceRecord>().HasKey(e => e.CorrespondenceId);
        modelBuilder.Entity<AuditEntry>().HasKey(e => e.AuditEntryId);
        modelBuilder.Entity<LetterTemplate>().HasKey(e => e.LetterType);

        // Relationships
        modelBuilder.Entity<Estate>()
            .HasOne(e => e.Controller)
            .WithMany(c => c.AssignedEstates)
            .HasForeignKey(e => e.ControllerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Deceased>()
            .HasOne(d => d.Estate)
            .WithOne(e => e.Deceased)
            .HasForeignKey<Deceased>(d => d.EstateId);

        modelBuilder.Entity<Executor>()
            .HasOne(x => x.Estate).WithMany(e => e.Executors)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Beneficiary>()
            .HasOne(x => x.Estate).WithMany(e => e.Beneficiaries)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImmovableProperty>()
            .HasOne(x => x.Estate).WithMany(e => e.ImmovableProperties)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovableAsset>()
            .HasOne(x => x.Estate).WithMany(e => e.MovableAssets)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FinancialClaim>()
            .HasOne(x => x.Estate).WithMany(e => e.FinancialClaims)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Liability>()
            .HasOne(x => x.Estate).WithMany(e => e.Liabilities)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DiaryEntry>()
            .HasOne(x => x.Estate).WithMany(e => e.DiaryEntries)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProgressStage>()
            .HasOne(x => x.Estate).WithMany(e => e.ProgressStages)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScannedDocument>()
            .HasOne(x => x.Estate).WithMany(e => e.ScannedDocuments)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AutomatedReport>()
            .HasOne(x => x.Estate).WithMany(e => e.AutomatedReports)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EstateMilestone>()
            .HasOne(x => x.Estate).WithMany(e => e.Milestones)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EstateMilestone>()
            .HasIndex(x => new { x.EstateId, x.Stage })
            .IsUnique();

        modelBuilder.Entity<CorrespondenceRecord>()
            .HasOne(x => x.Estate).WithMany(e => e.Correspondence)
            .HasForeignKey(x => x.EstateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CorrespondenceRecord>()
            .HasIndex(x => new { x.EstateId, x.SentAtUtc });

        // Audit entries are kept even if the estate is later deleted, so no FK.
        modelBuilder.Entity<AuditEntry>()
            .HasIndex(x => new { x.EstateId, x.TimestampUtc });

        modelBuilder.Entity<AuditEntry>()
            .HasIndex(x => x.TimestampUtc);

        // Short, indexable string columns for the new tables.
        modelBuilder.Entity<EstateMilestone>().Property(x => x.Stage)
            .HasConversion<string>().HasMaxLength(40);
        modelBuilder.Entity<CorrespondenceRecord>().Property(x => x.Channel).HasMaxLength(20);
        modelBuilder.Entity<CorrespondenceRecord>().Property(x => x.Status).HasMaxLength(20);
        modelBuilder.Entity<AuditEntry>().Property(x => x.Action).HasMaxLength(20);
        modelBuilder.Entity<LetterTemplate>().Property(x => x.LetterType).HasMaxLength(50);

        // Statuses are enums in code but stay as the same text in the
        // database. Unknown legacy values fall back to a default instead of
        // breaking the whole estate load.
        modelBuilder.Entity<DiaryEntry>().Property(x => x.NotifyBy)
            .HasConversion(new ValueConverter<NotifyMethod, string>(
                v => v.ToString(),
                v => ParseNotifyMethod(v)));

        modelBuilder.Entity<ProgressStage>().Property(x => x.Status)
            .HasConversion(new ValueConverter<ProgressStatus, string>(
                v => v.ToString(),
                v => ParseProgressStatus(v)));

        // Encryption at rest for personal identifiers (POPIA).
        if (_fieldProtector is not null)
        {
            var protector = _fieldProtector;
            var encrypted = new ValueConverter<string, string>(
                v => protector.Protect(v),
                v => protector.Unprotect(v));

            modelBuilder.Entity<Deceased>().Property(x => x.IdNumber).HasConversion(encrypted);
            modelBuilder.Entity<Executor>().Property(x => x.IdNumber).HasConversion(encrypted);
            modelBuilder.Entity<Beneficiary>().Property(x => x.IdNumber).HasConversion(encrypted);
            modelBuilder.Entity<FinancialClaim>().Property(x => x.AccountReference).HasConversion(encrypted);
        }

        // Precision for all decimal (money) fields
        foreach (var prop in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal)))
        {
            prop.SetColumnType("decimal(18,2)");
        }
    }

    private static NotifyMethod ParseNotifyMethod(string value) =>
        StatusParsing.TryParseNotifyMethod(value, out var method) ? method : NotifyMethod.Email;

    private static ProgressStatus ParseProgressStatus(string value) =>
        StatusParsing.TryParseProgressStatus(value, out var status) ? status : ProgressStatus.Pending;
}

internal sealed class FieldProtectorModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is EstateFlowDbContext estateContext
            ? (context.GetType(), estateContext.FieldProtectorKeyId, designTime)
            : (object)(context.GetType(), designTime);
}

using EstateFlow.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Data.Context;

public class EstateFlowDbContext : IdentityDbContext<ApplicationUser>
{
    public EstateFlowDbContext(DbContextOptions<EstateFlowDbContext> options)
        : base(options) { }

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

        // Precision for all decimal (money) fields
        foreach (var prop in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal)))
        {
            prop.SetColumnType("decimal(18,2)");
        }
    }
}
using EstateFlow.Core.Interfaces;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Data.Repositories;

public class EstateRepository : IEstateRepository
{
    private readonly EstateFlowDbContext _context;

    public EstateRepository(
        EstateFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Estate?> GetByIdAsync(
        Guid estateId)
    {
        return await _context.Estates
            .Include(e => e.Deceased)
            .Include(e => e.Executors)
            .Include(e => e.Beneficiaries)
            .Include(e => e.ImmovableProperties)
            .Include(e => e.MovableAssets)
            .Include(e => e.FinancialClaims)
            .Include(e => e.Liabilities)
            .Include(e => e.DiaryEntries)
            .Include(e => e.ProgressStages)
            .Include(e => e.ScannedDocuments)
            .Include(e => e.AutomatedReports)
            .Include(e => e.Milestones)
            .Include(e => e.Controller)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                e => e.EstateId == estateId);
    }

    public async Task<List<Estate>> GetAllAsync()
    {
        return await _context.Estates
            .Include(e => e.Deceased)
            .Include(e => e.Controller)
            .Include(e => e.DiaryEntries)
            .Include(e => e.AutomatedReports)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Estate>>
        GetByControllerIdAsync(
            Guid controllerId)
    {
        return await _context.Estates
            .Where(e =>
                e.ControllerId == controllerId)
            .Include(e => e.Deceased)
            .Include(e => e.Controller)
            .Include(e => e.DiaryEntries)
            .Include(e => e.AutomatedReports)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Estate> AddAsync(
        Estate estate)
    {
        await _context.Estates.AddAsync(estate);

        await _context.SaveChangesAsync();

        return estate;
    }

    public async Task SaveAsync(
        Estate estate)
    {
        // Estate returned by GetByIdAsync is tracked.
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        Guid estateId)
    {
        var estate =
            await _context.Estates
                .FirstOrDefaultAsync(
                    e => e.EstateId == estateId);

        if (estate is null)
            return;

        _context.Estates.Remove(estate);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(
        Guid estateId)
    {
        return await _context.Estates
            .AsNoTracking()
            .AnyAsync(
                e => e.EstateId == estateId);
    }

    public async Task<bool> IsAssignedToControllerAsync(
        Guid estateId,
        Guid controllerId)
    {
        return await _context.Estates
            .AsNoTracking()
            .AnyAsync(
                e => e.EstateId == estateId &&
                     e.ControllerId == controllerId);
    }

    // =====================================================
    // EXPLICIT CHILD INSERTS
    // =====================================================

    public async Task AddExecutorAsync(
        Executor executor)
    {
        _context.Entry(executor).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddBeneficiaryAsync(
        Beneficiary beneficiary)
    {
        _context.Entry(beneficiary).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddMovableAssetAsync(
        MovableAsset asset)
    {
        _context.Entry(asset).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddImmovablePropertyAsync(
        ImmovableProperty property)
    {
        _context.Entry(property).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddFinancialClaimAsync(
        FinancialClaim claim)
    {
        _context.Entry(claim).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddDiaryEntryAsync(
        DiaryEntry diaryEntry)
    {
        _context.Entry(diaryEntry).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddProgressStageAsync(
        ProgressStage progressStage)
    {
        _context.Entry(progressStage).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddLiabilityAsync(
        Liability liability)
    {
        _context.Entry(liability).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddMilestoneAsync(
        EstateMilestone milestone)
    {
        _context.Entry(milestone).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }

    public async Task AddAutomatedReportAsync(
        AutomatedReport report)
    {
        _context.Entry(report).State =
            EntityState.Added;

        await _context.SaveChangesAsync();
    }
}

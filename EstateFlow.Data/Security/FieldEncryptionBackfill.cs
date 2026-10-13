using EstateFlow.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Data.Security;

// When field encryption is switched on, rows written earlier are still plain
// text. This finds them and saves them again so they are encrypted. Safe to
// run on every start-up: once everything is encrypted it finds nothing.
public static class FieldEncryptionBackfill
{
    public static async Task<int> RunAsync(EstateFlowDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
            return 0;

        var pattern = AesGcmFieldProtector.Prefix + "%";
        var updated = 0;

        var deceasedIds = await context.Database
            .SqlQuery<Guid>($"SELECT DeceasedId AS Value FROM Deceased WHERE IdNumber <> '' AND IdNumber NOT LIKE {pattern}")
            .ToListAsync(cancellationToken);
        foreach (var row in await context.Deceased.Where(x => deceasedIds.Contains(x.DeceasedId)).ToListAsync(cancellationToken))
        {
            context.Entry(row).Property(x => x.IdNumber).IsModified = true;
            updated++;
        }

        var executorIds = await context.Database
            .SqlQuery<Guid>($"SELECT ExecutorId AS Value FROM Executors WHERE IdNumber <> '' AND IdNumber NOT LIKE {pattern}")
            .ToListAsync(cancellationToken);
        foreach (var row in await context.Executors.Where(x => executorIds.Contains(x.ExecutorId)).ToListAsync(cancellationToken))
        {
            context.Entry(row).Property(x => x.IdNumber).IsModified = true;
            updated++;
        }

        var beneficiaryIds = await context.Database
            .SqlQuery<Guid>($"SELECT BeneficiaryId AS Value FROM Beneficiaries WHERE IdNumber <> '' AND IdNumber NOT LIKE {pattern}")
            .ToListAsync(cancellationToken);
        foreach (var row in await context.Beneficiaries.Where(x => beneficiaryIds.Contains(x.BeneficiaryId)).ToListAsync(cancellationToken))
        {
            context.Entry(row).Property(x => x.IdNumber).IsModified = true;
            updated++;
        }

        var claimIds = await context.Database
            .SqlQuery<Guid>($"SELECT ClaimId AS Value FROM FinancialClaims WHERE AccountReference <> '' AND AccountReference NOT LIKE {pattern}")
            .ToListAsync(cancellationToken);
        foreach (var row in await context.FinancialClaims.Where(x => claimIds.Contains(x.ClaimId)).ToListAsync(cancellationToken))
        {
            context.Entry(row).Property(x => x.AccountReference).IsModified = true;
            updated++;
        }

        if (updated > 0)
            await context.SaveChangesAsync(cancellationToken);

        return updated;
    }
}

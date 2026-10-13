using EstateFlow.Core.Dtos;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Api.Services.Correspondence;

// The estate's correspondence history: every email and letter sent from
// EstateFlow, with who sent it, to whom and when.
public interface ICorrespondenceLog
{
    Task RecordAsync(CorrespondenceRecord record);

    Task<List<CorrespondenceDto>> GetForEstateAsync(Guid estateId);
}

public class CorrespondenceLog : ICorrespondenceLog
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EstateFlowDbContext _context;
    private readonly ILogger<CorrespondenceLog> _logger;

    public CorrespondenceLog(
        IServiceScopeFactory scopeFactory,
        EstateFlowDbContext context,
        ILogger<CorrespondenceLog> logger)
    {
        _scopeFactory = scopeFactory;
        _context = context;
        _logger = logger;
    }

    // Separate scope: recording must not save unrelated pending changes,
    // and a failure to record must not undo the email that was already sent.
    public async Task RecordAsync(CorrespondenceRecord record)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EstateFlowDbContext>();
            db.Correspondence.Add(record);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not record correspondence for estate {EstateId}.", record.EstateId);
        }
    }

    public async Task<List<CorrespondenceDto>> GetForEstateAsync(Guid estateId)
    {
        return await _context.Correspondence
            .AsNoTracking()
            .Where(c => c.EstateId == estateId)
            .OrderByDescending(c => c.SentAtUtc)
            .Select(c => new CorrespondenceDto
            {
                CorrespondenceId = c.CorrespondenceId,
                SentAtUtc = c.SentAtUtc,
                Channel = c.Channel,
                Kind = c.Kind,
                SentBy = c.SentBy,
                RecipientName = c.RecipientName,
                RecipientEmail = c.RecipientEmail,
                Subject = c.Subject,
                Summary = c.Summary,
                Status = c.Status,
                DocumentId = c.DocumentId
            })
            .ToListAsync();
    }
}

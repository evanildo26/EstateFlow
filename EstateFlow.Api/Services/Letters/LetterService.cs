using EstateFlow.Api.Services.Correspondence;
using EstateFlow.Api.Services.Storage;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.Extensions.Options;

namespace EstateFlow.Api.Services.Letters;

public record LetterResult(GeneratedLetter Letter, bool SavedToEstate, Guid? DocumentId);

// Generates a letter, files a copy on the estate's documents and records it
// in the correspondence history, so there is always a record of what was sent.
public class LetterService
{
    private readonly LetterGeneratorFactory _factory;
    private readonly ILetterTemplateStore _templates;
    private readonly IBlobStorageService _blobStorage;
    private readonly EstateFlowDbContext _context;
    private readonly ICorrespondenceLog _correspondence;
    private readonly FirmOptions _firm;
    private readonly ILogger<LetterService> _logger;

    public LetterService(
        LetterGeneratorFactory factory,
        ILetterTemplateStore templates,
        IBlobStorageService blobStorage,
        EstateFlowDbContext context,
        ICorrespondenceLog correspondence,
        IOptions<FirmOptions> firm,
        ILogger<LetterService> logger)
    {
        _factory = factory;
        _templates = templates;
        _blobStorage = blobStorage;
        _context = context;
        _correspondence = correspondence;
        _firm = firm.Value;
        _logger = logger;
    }

    public async Task<LetterResult> GenerateAsync(
        Estate estate,
        string letterType,
        object sourceRecord,
        string kindLabel,
        string generatedBy)
    {
        var generator = _factory.Create(letterType);
        var draft = generator.Draft(estate, sourceRecord);
        var (subjectTemplate, bodyTemplate) = await _templates.GetAsync(generator.LetterType);

        var subject = LetterTemplateStore.Fill(subjectTemplate, draft.Values);
        var body = LetterTemplateStore.Fill(bodyTemplate, draft.Values);

        var data = DocxLetterBuilder.Build(new DocxLetterBuilder.LetterContent(
            _firm,
            estate.FileNumber,
            draft.Values.GetValueOrDefault("MastersReference", string.Empty),
            draft.RecipientLines,
            subject,
            body,
            string.IsNullOrWhiteSpace(estate.Controller?.StaffName) ? generatedBy : estate.Controller!.StaffName,
            estate.Controller?.Email ?? string.Empty));

        var fileName = SafeFileName($"{estate.FileNumber}_{draft.FileNameStem}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");

        var letter = new GeneratedLetter
        {
            FileName = fileName,
            Data = data,
            LetterType = generator.LetterType,
            Subject = subject,
            RecipientName = draft.RecipientName
        };

        var documentId = await TryFileOnEstateAsync(estate, letter, kindLabel);

        await _correspondence.RecordAsync(new CorrespondenceRecord
        {
            EstateId = estate.EstateId,
            Channel = "Letter",
            Kind = kindLabel,
            SentBy = generatedBy,
            RecipientName = draft.RecipientName,
            Subject = subject,
            Summary = documentId is null
                ? "Letter generated (copy could not be filed on the estate)."
                : $"Letter generated and filed as {fileName}.",
            Status = "Generated",
            DocumentId = documentId
        });

        return new LetterResult(letter, documentId is not null, documentId);
    }

    // Filing is best-effort: if Blob Storage is unavailable the controller
    // still gets the letter, and the correspondence log says it wasn't filed.
    private async Task<Guid?> TryFileOnEstateAsync(Estate estate, GeneratedLetter letter, string kindLabel)
    {
        var documentId = Guid.NewGuid();
        var blobReference = $"{estate.EstateId:N}/{letter.FileName}";

        try
        {
            using var stream = new MemoryStream(letter.Data);
            await _blobStorage.UploadAsync(stream, blobReference, letter.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not file generated letter on estate {EstateId}.", estate.EstateId);
            return null;
        }

        _context.ScannedDocuments.Add(new ScannedDocument
        {
            DocumentId = documentId,
            EstateId = estate.EstateId,
            DocumentType = $"Letter: {kindLabel}",
            FileName = letter.FileName,
            UploadDate = DateTime.UtcNow,
            BlobReference = blobReference
        });

        await _context.SaveChangesAsync();
        return documentId;
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
    }
}

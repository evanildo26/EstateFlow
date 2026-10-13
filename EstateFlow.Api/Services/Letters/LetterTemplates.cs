using System.Globalization;
using System.Text.RegularExpressions;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EstateFlow.Api.Services.Letters;

public static class LetterTypes
{
    public const string VehicleTransfer = "VehicleTransfer";
    public const string InstitutionNotification = "InstitutionNotification";
}

// The values available to every letter template.
public static class LetterValues
{
    public static readonly string[] Common =
    {
        "FileNumber", "MastersReference", "DeceasedName", "DeceasedIdNumber",
        "DateOfDeath", "ControllerName", "ControllerEmail", "Today"
    };

    public static LetterDraft ForEstate(Estate estate)
    {
        var culture = CultureInfo.GetCultureInfo("en-ZA");

        return new LetterDraft
        {
            Values = new Dictionary<string, string>
            {
                ["FileNumber"] = estate.FileNumber,
                ["MastersReference"] = string.IsNullOrWhiteSpace(estate.MastersReference) ? "(to be advised)" : estate.MastersReference,
                ["DeceasedName"] = estate.Deceased?.FullName ?? string.Empty,
                ["DeceasedIdNumber"] = estate.Deceased?.IdNumber ?? string.Empty,
                ["DateOfDeath"] = estate.DateOfDeath.ToString("d MMMM yyyy", culture),
                ["ControllerName"] = estate.Controller?.StaffName ?? string.Empty,
                ["ControllerEmail"] = estate.Controller?.Email ?? string.Empty,
                ["Today"] = DateTime.Today.ToString("d MMMM yyyy", culture)
            }
        };
    }
}

public record LetterTemplateDefinition(
    string LetterType,
    string DisplayName,
    string Subject,
    string Body,
    string[] ExtraPlaceholders);

// Built-in wording, used until a Firm Administrator customises a template.
// The body is plain text: blank lines separate paragraphs.
public static class DefaultLetterTemplates
{
    public static readonly IReadOnlyList<LetterTemplateDefinition> All = new[]
    {
        new LetterTemplateDefinition(
            LetterTypes.VehicleTransfer,
            "Vehicle transfer — Traffic Department",
            "TRANSFER OF VEHICLE OWNERSHIP: ESTATE LATE {DeceasedName}",
            """
            We act on behalf of the executor in the above deceased estate. The deceased, {DeceasedName} (identity number {DeceasedIdNumber}), passed away on {DateOfDeath}.

            The following vehicle is registered in the name of the deceased:

            Description: {VehicleDescription}
            Make: {VehicleMake}
            Registration number: {RegistrationNumber}
            VIN: {VinNumber}

            We kindly request that the above vehicle be transferred into the name of the deceased estate pending distribution to the appointed heir, in accordance with the Administration of Estates Act 66 of 1965.

            Should you require any further documentation, please contact the writer, quoting our reference {FileNumber}.
            """,
            new[] { "VehicleDescription", "VehicleMake", "RegistrationNumber", "VinNumber" }),

        new LetterTemplateDefinition(
            LetterTypes.InstitutionNotification,
            "Notification of death — bank / insurer",
            "NOTIFICATION OF DEATH: ESTATE LATE {DeceasedName}",
            """
            We act on behalf of the executor in the above deceased estate and hereby notify you of the death of {DeceasedName} (identity number {DeceasedIdNumber}), who passed away on {DateOfDeath}.

            Our records reflect the following account or policy held with you:

            Account / policy reference: {AccountReference}
            Estimated value: {ClaimValue}

            Kindly freeze the account, confirm the balance held as at the date of death, and provide the documentation required to release these funds to the deceased estate.

            Please direct all correspondence to the writer, quoting our reference {FileNumber} and the Master's reference {MastersReference}.
            """,
            new[] { "InstitutionName", "InstitutionAddress", "AccountReference", "ClaimValue" }),
    };

    public static LetterTemplateDefinition Get(string letterType) =>
        All.FirstOrDefault(t => t.LetterType == letterType)
        ?? throw new NotSupportedException($"Unknown letter type '{letterType}'.");
}

public interface ILetterTemplateStore
{
    Task<(string Subject, string Body)> GetAsync(string letterType);
    Task<List<LetterTemplateDto>> ListAsync();
    Task<LetterTemplateDto> SaveAsync(string letterType, UpdateLetterTemplateRequest request, string updatedBy);
    Task<LetterTemplateDto> ResetAsync(string letterType);
}

public class LetterTemplateStore : ILetterTemplateStore
{
    private readonly EstateFlowDbContext _context;

    public LetterTemplateStore(EstateFlowDbContext context)
    {
        _context = context;
    }

    public async Task<(string Subject, string Body)> GetAsync(string letterType)
    {
        var custom = await _context.LetterTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.LetterType == letterType);

        if (custom is not null)
            return (custom.Subject, custom.Body);

        var builtIn = DefaultLetterTemplates.Get(letterType);
        return (builtIn.Subject, builtIn.Body);
    }

    public async Task<List<LetterTemplateDto>> ListAsync()
    {
        var custom = await _context.LetterTemplates.AsNoTracking().ToListAsync();

        return DefaultLetterTemplates.All
            .Select(d => ToDto(d, custom.FirstOrDefault(c => c.LetterType == d.LetterType)))
            .ToList();
    }

    public async Task<LetterTemplateDto> SaveAsync(string letterType, UpdateLetterTemplateRequest request, string updatedBy)
    {
        var definition = DefaultLetterTemplates.Get(letterType);

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
            throw new ArgumentException("Both the subject and the body are required.");

        var allowed = LetterValues.Common.Concat(definition.ExtraPlaceholders).ToHashSet(StringComparer.Ordinal);
        var unknown = Placeholders(request.Subject + request.Body).Where(p => !allowed.Contains(p)).Distinct().ToList();

        if (unknown.Count > 0)
        {
            throw new ArgumentException(
                $"Unknown placeholder(s): {string.Join(", ", unknown.Select(p => "{" + p + "}"))}. " +
                $"Available: {string.Join(", ", allowed.Select(p => "{" + p + "}"))}.");
        }

        var row = await _context.LetterTemplates.FirstOrDefaultAsync(t => t.LetterType == letterType);
        if (row is null)
        {
            row = new LetterTemplate { LetterType = letterType };
            _context.LetterTemplates.Add(row);
        }

        row.Subject = request.Subject.Trim();
        row.Body = request.Body.Trim();
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        return ToDto(definition, row);
    }

    public async Task<LetterTemplateDto> ResetAsync(string letterType)
    {
        var definition = DefaultLetterTemplates.Get(letterType);

        var row = await _context.LetterTemplates.FirstOrDefaultAsync(t => t.LetterType == letterType);
        if (row is not null)
        {
            _context.LetterTemplates.Remove(row);
            await _context.SaveChangesAsync();
        }

        return ToDto(definition, null);
    }

    public static string Fill(string text, IReadOnlyDictionary<string, string> values) =>
        PlaceholderPattern.Replace(text, m =>
            values.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);

    private static readonly Regex PlaceholderPattern = new(@"\{([A-Za-z]+)\}", RegexOptions.Compiled);

    private static IEnumerable<string> Placeholders(string text) =>
        PlaceholderPattern.Matches(text).Select(m => m.Groups[1].Value);

    private static LetterTemplateDto ToDto(LetterTemplateDefinition definition, LetterTemplate? custom) => new()
    {
        LetterType = definition.LetterType,
        DisplayName = definition.DisplayName,
        Subject = custom?.Subject ?? definition.Subject,
        Body = custom?.Body ?? definition.Body,
        IsCustomised = custom is not null,
        UpdatedAtUtc = custom?.UpdatedAtUtc,
        UpdatedBy = custom?.UpdatedBy,
        Placeholders = LetterValues.Common.Concat(definition.ExtraPlaceholders).Select(p => "{" + p + "}").ToList()
    };
}

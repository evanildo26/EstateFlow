using EstateFlow.Core.Models;

namespace EstateFlow.Api.Services.Letters;

// Notification-of-death letter to a bank or insurer,
// per client requirement F in the documentation.
public class InstitutionLetterGenerator : IDocumentGenerator
{
    public string LetterType => LetterTypes.InstitutionNotification;

    public LetterDraft Draft(Estate estate, object sourceRecord)
    {
        if (sourceRecord is not FinancialClaim claim)
            throw new InvalidOperationException("InstitutionLetterGenerator requires a FinancialClaim.");

        var draft = LetterValues.ForEstate(estate);
        draft.RecipientName = claim.InstitutionName;
        draft.RecipientLines = new() { claim.InstitutionName };
        draft.RecipientLines.AddRange(
            (claim.InstitutionAddress ?? string.Empty)
                .Split(new[] { '\n', ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        draft.FileNameStem = claim.InstitutionName.Replace(" ", string.Empty);

        draft.Values["InstitutionName"] = claim.InstitutionName;
        draft.Values["InstitutionAddress"] = claim.InstitutionAddress ?? string.Empty;
        draft.Values["AccountReference"] = claim.AccountReference;
        draft.Values["ClaimValue"] = $"R {claim.Value:N2}";

        return draft;
    }
}

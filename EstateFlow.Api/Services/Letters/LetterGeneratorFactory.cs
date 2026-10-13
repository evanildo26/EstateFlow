namespace EstateFlow.Api.Services.Letters;

// Factory Method pattern — picks the correct IDocumentGenerator based on
// the type of record being actioned, so new letter types (e.g. a future
// Deeds Office letter) can be added without changing any calling code.
public class LetterGeneratorFactory
{
    public IDocumentGenerator Create(string letterType)
    {
        return letterType switch
        {
            "Vehicle" or LetterTypes.VehicleTransfer => new TrafficDeptLetterGenerator(),
            "FinancialClaim" or LetterTypes.InstitutionNotification => new InstitutionLetterGenerator(),
            _ => throw new NotSupportedException($"No letter generator registered for type '{letterType}'.")
        };
    }
}

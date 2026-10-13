using EstateFlow.Core.Models;

namespace EstateFlow.Api.Services.Letters;

// Vehicle-transfer letter to the Municipality Traffic Department,
// per client requirement F in the documentation.
public class TrafficDeptLetterGenerator : IDocumentGenerator
{
    public string LetterType => LetterTypes.VehicleTransfer;

    public LetterDraft Draft(Estate estate, object sourceRecord)
    {
        if (sourceRecord is not MovableAsset asset || !asset.IsVehicle)
            throw new InvalidOperationException("TrafficDeptLetterGenerator requires a vehicle MovableAsset.");

        var draft = LetterValues.ForEstate(estate);
        draft.RecipientName = "Municipality Traffic Department";
        draft.RecipientLines = new() { "The Registering Authority", "Municipality Traffic Department" };
        draft.FileNameStem = $"TrafficDept_{asset.RegistrationNumber}";

        draft.Values["VehicleDescription"] = asset.Description;
        draft.Values["VehicleMake"] = asset.Make ?? string.Empty;
        draft.Values["RegistrationNumber"] = asset.RegistrationNumber ?? string.Empty;
        draft.Values["VinNumber"] = asset.VinNumber ?? string.Empty;

        return draft;
    }
}

namespace EstateFlow.Api.Services.Letters;

// Letterhead details, from the "Firm" configuration section
// (App Service settings Firm__Name, Firm__Address, ...).
public class FirmOptions
{
    public const string SectionName = "Firm";

    public string Name { get; set; } = "STBB Incorporated";
    public string Division { get; set; } = "Deceased Estates Division";

    // Multi-line; use "\n" or "|" between lines.
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}

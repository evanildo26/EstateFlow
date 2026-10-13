namespace EstateFlow.Core.Validation;

// South African ID numbers: YYMMDD SSSS C A Z (13 digits)
//   YYMMDD  date of birth
//   SSSS    gender sequence (0000-4999 female, 5000-9999 male)
//   C       citizenship (0 = SA citizen, 1 = permanent resident, 2 = refugee)
//   A       historically race, now always 8 or 9 (not validated)
//   Z       Luhn check digit
//
// A typo in an ID number ends up in letters to banks and the Master, so the
// API rejects numbers that look like an SA ID (all digits) but fail these
// checks. Values containing letters are treated as passport numbers and are
// accepted as-is, because executors and heirs may be foreign nationals.
public static class SouthAfricanIdNumber
{
    public static IdNumberCheck Check(string? value)
    {
        var trimmed = value?.Replace(" ", string.Empty).Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            return IdNumberCheck.Empty;

        if (!trimmed.All(char.IsDigit))
            return IdNumberCheck.Passport(trimmed);

        if (trimmed.Length != 13)
            return IdNumberCheck.Invalid("A South African ID number must have exactly 13 digits.");

        var dateOfBirth = TryGetDateOfBirth(trimmed);
        if (dateOfBirth is null)
            return IdNumberCheck.Invalid("The first six digits of the ID number are not a valid date of birth (YYMMDD).");

        if (trimmed[10] is not ('0' or '1' or '2'))
            return IdNumberCheck.Invalid("The 11th digit of the ID number must be 0, 1 or 2.");

        if (!PassesLuhn(trimmed))
            return IdNumberCheck.Invalid("The ID number's check digit is wrong — please re-check it for a typo.");

        return IdNumberCheck.Valid(trimmed, dateOfBirth.Value);
    }

    // Two-digit years: anything later than this year is taken as 19xx.
    private static DateTime? TryGetDateOfBirth(string id)
    {
        var yy = int.Parse(id[..2]);
        var mm = int.Parse(id.Substring(2, 2));
        var dd = int.Parse(id.Substring(4, 2));

        var currentTwoDigitYear = DateTime.Today.Year % 100;
        var year = yy <= currentTwoDigitYear ? 2000 + yy : 1900 + yy;

        if (mm is < 1 or > 12 || dd < 1 || dd > DateTime.DaysInMonth(year, mm))
            return null;

        return new DateTime(year, mm, dd);
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleIt = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleIt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }

            sum += d;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}

public sealed record IdNumberCheck(
    bool IsAcceptable,
    bool IsSouthAfricanId,
    string Normalized,
    DateTime? DateOfBirth,
    string? Error)
{
    public static IdNumberCheck Empty { get; } = new(true, false, string.Empty, null, null);

    public static IdNumberCheck Passport(string value) => new(true, false, value.ToUpperInvariant(), null, null);

    public static IdNumberCheck Valid(string value, DateTime dateOfBirth) => new(true, true, value, dateOfBirth, null);

    public static IdNumberCheck Invalid(string error) => new(false, false, string.Empty, null, error);
}

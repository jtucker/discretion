using System.Text.RegularExpressions;

namespace Discretion.Services;

public static partial class Validation
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
    private static partial Regex EmailRegex();

    public static bool IsEmail(string? value) => !string.IsNullOrWhiteSpace(value) && EmailRegex().IsMatch(value.Trim());

    public static bool IsPhone(string? value) => (value ?? "").Count(char.IsDigit) is >= 7 and <= 15;
}

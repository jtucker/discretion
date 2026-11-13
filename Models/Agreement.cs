namespace Discretion.Models;

public enum TermUnit
{
    Months,
    Years
}

public enum AgreementStatus
{
    AwaitingSignatures,
    PartiallySigned,
    Executed,
    Expired
}

public sealed class PartyInfo
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public string PostalCode { get; set; } = "";

    public string FormattedAddress
    {
        get
        {
            var cityLine = string.Join(" ", new[] { string.Join(", ", new[] { City, Region }.Where(s => !string.IsNullOrWhiteSpace(s))), PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join(", ", new[] { Street, cityLine }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}

public sealed class SignaturePoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class SignatureRecord
{
    public List<List<SignaturePoint>> Strokes { get; set; } = [];
    public float CanvasWidth { get; set; }
    public float CanvasHeight { get; set; }
    public string SignedName { get; set; } = "";
    public DateTime SignedAt { get; set; }
    public string Device { get; set; } = "";
    public string DocumentFingerprint { get; set; } = "";

    public bool IsEmpty => Strokes.Count == 0 || Strokes.All(s => s.Count == 0);
}

public sealed class Agreement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = "";
    public string TemplateId { get; set; } = "";
    public string TemplateTitle { get; set; } = "";
    public int TermLength { get; set; }
    public TermUnit TermUnit { get; set; }
    public bool SurvivesExpiration { get; set; }
    public PartyInfo DisclosingParty { get; set; } = new();
    public PartyInfo ReceivingParty { get; set; } = new();
    public string GoverningState { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public SignatureRecord? DisclosingSignature { get; set; }
    public SignatureRecord? ReceivingSignature { get; set; }
    public string? DocumentFileName { get; set; }

    public DateTime ExpirationDate => TermUnit == TermUnit.Years
        ? EffectiveDate.AddYears(TermLength)
        : EffectiveDate.AddMonths(TermLength);

    public string TermDescription => FormatTerm(TermLength, TermUnit);

    public bool IsFullyExecuted => DisclosingSignature is not null && ReceivingSignature is not null;

    public AgreementStatus Status
    {
        get
        {
            if (IsFullyExecuted)
                return DateTime.Today >= ExpirationDate.Date ? AgreementStatus.Expired : AgreementStatus.Executed;
            return DisclosingSignature is null && ReceivingSignature is null
                ? AgreementStatus.AwaitingSignatures
                : AgreementStatus.PartiallySigned;
        }
    }

    public string StatusText => Status switch
    {
        AgreementStatus.AwaitingSignatures => "Awaiting Signatures",
        AgreementStatus.PartiallySigned => "Partially Signed",
        AgreementStatus.Executed => "Executed",
        AgreementStatus.Expired => "Expired",
        _ => ""
    };

    public static string FormatTerm(int length, TermUnit unit)
    {
        var word = unit == TermUnit.Years ? "year" : "month";
        return $"{NumberWords.ToWords(length)} ({length}) {word}{(length == 1 ? "" : "s")}";
    }
}

public static class NumberWords
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    public static string ToWords(int n)
    {
        if (n < 0 || n > 999) return n.ToString();
        if (n < 20) return Ones[n];
        if (n < 100) return Tens[n / 10] + (n % 10 == 0 ? "" : "-" + Ones[n % 10]);
        return Ones[n / 100] + " hundred" + (n % 100 == 0 ? "" : " " + ToWords(n % 100));
    }
}

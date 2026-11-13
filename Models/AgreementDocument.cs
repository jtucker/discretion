namespace Discretion.Models;

public enum RunStyle
{
    Regular,
    Bold,
    Italic
}

public sealed record TextRun(string Text, RunStyle Style = RunStyle.Regular);

public abstract record DocBlock;

public sealed record TitleBlock(string Title, string Subtitle, string Reference) : DocBlock;

public sealed record SectionHeadingBlock(string Text) : DocBlock;

public sealed record ParagraphBlock(IReadOnlyList<TextRun> Runs, bool Centered = false, float Indent = 0) : DocBlock
{
    public ParagraphBlock(string text, RunStyle style = RunStyle.Regular, bool centered = false)
        : this([new TextRun(text, style)], centered) { }
}

public sealed record SignatureBlock(string Role, PartyInfo Party, SignatureRecord? Signature) : DocBlock;

public sealed class AgreementDocument
{
    public required string ReferenceNumber { get; init; }
    public required string Fingerprint { get; init; }
    public required IReadOnlyList<DocBlock> Blocks { get; init; }
}

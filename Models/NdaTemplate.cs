namespace Discretion.Models;

public sealed class NdaTemplate
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string Monogram { get; init; }
    public required string Summary { get; init; }

    /// <summary>Completes the sentence "WHEREAS, ..." in the recitals.</summary>
    public required string Recital { get; init; }

    /// <summary>Completes the sentence "'Confidential Information' means ...".</summary>
    public required string Definition { get; init; }

    public required IReadOnlyList<string> SpecialCovenants { get; init; }
}

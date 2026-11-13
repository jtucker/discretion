namespace Discretion.Services;

public static class UsStates
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware",
        "District of Columbia", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas",
        "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi",
        "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York",
        "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island",
        "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington",
        "West Virginia", "Wisconsin", "Wyoming"
    ];

    /// <summary>Formats the jurisdiction for use in a sentence, e.g. "the State of Texas".</summary>
    public static string Jurisdiction(string state) => state switch
    {
        "District of Columbia" => "the District of Columbia",
        "Massachusetts" or "Pennsylvania" or "Virginia" or "Kentucky" => $"the Commonwealth of {state}",
        _ => $"the State of {state}"
    };
}

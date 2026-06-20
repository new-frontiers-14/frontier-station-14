using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Shipyard.Prototypes;

[Prototype]
public sealed class SuffixGeneratorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Format string to combine all inputs into final suffix
    /// </summary>
    [DataField(required: true)]
    public string Format = default!;

    /// <summary>
    /// If a ship does not specify a designator, use this to generate one.
    /// If the suffix generator doesn't specify, fallback to the Nanotrasen name generator ones
    /// </summary>
    [DataField]
    public SuffixGeneratorInputEntry DefaultDesignator = new()
    {
        Type = SuffixGeneratorInputType.List,
        Items = ["LV", "NX", "EV", "QT", "PR"] // The OG Suffix Prefixes
    };

    /// <summary>
    /// Different 'parts' of the suffix, each specifing how they generate
    /// </summary>
    [DataField(required: true)]
    public List<SuffixGeneratorInputEntry> Designators = [];

}

[DataDefinition]
public readonly partial record struct SuffixGeneratorInputEntry()
{
    /// <summary>
    /// Generator type, determines which function is called to make up part of the string
    /// </summary>
    [DataField(required: true)]
    public SuffixGeneratorInputType Type { get; init; } = default!;
    /// <summary>
    /// Used by all but List type, specifies the minimum characters the input produces, or for numeric the minimum value
    /// </summary>
    [DataField]
    public int? Min { get; init; } = default;
    /// <summary>
    /// Used by all but List type, specifies the maximum characters the input produces, or for numeric the maximum value
    /// </summary>
    [DataField]
    public int? Max { get; init; } = default;
    /// <summary>
    /// Only used by the numeric type, defaults to just normal decimal representation but any .NET number format specifier can be used
    /// </summary>
    [DataField]
    public string? Format { get; init; } = default;
    /// <summary>
    /// Only used by the List type, a random entry from the list of strings will be chosen
    /// </summary>
    [DataField]
    public string[]? Items { get; init; } = default;
}

public enum SuffixGeneratorInputType : byte
{
    Alpha,
    AlphaNumeric,
    Numeric,
    List
}

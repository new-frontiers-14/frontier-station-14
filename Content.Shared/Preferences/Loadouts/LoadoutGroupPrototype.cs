using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a set of loadouts for a particular slot.
/// </summary>
[Prototype("loadoutGroup")]
public sealed partial class LoadoutGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// User-friendly name for the group.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Minimum number of loadouts that need to be specified for this category.
    /// </summary>
    [DataField]
    public int MinLimit = 1;

    /// <summary>
    /// Maximum limit for the category.
    /// </summary>
    [DataField]
    public int MaxLimit = 1;

    /// <summary>
    /// Hides the loadout group from the player.
    /// </summary>
    [DataField]
    public bool Hidden;

    [DataField(required: true)]
    public List<ProtoId<LoadoutPrototype>> Loadouts = new();

    // Frontier: loadout redundancy
    /// <summary>
    /// Loadout subgroups - will be appended to loadout list.
    /// </summary>
    [DataField]
    public List<ProtoId<LoadoutGroupPrototype>> Subgroups = new();
    // End Frontier

    // Frontier: handle unaffordable loadouts
    /// <summary>
    /// Fallback loadouts to be selected in case a character cannot afford them.
    /// Also serves as a default loadout options (up to the maxLimit for a set) for a new character.
    /// </summary>
    [DataField]
    public List<ProtoId<LoadoutPrototype>> Fallbacks = new();
    // End Frontier
}

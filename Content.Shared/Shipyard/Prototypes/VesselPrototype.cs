using Content.Shared._NF.GameRule;
using Content.Shared.Guidebook;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Shipyard.Prototypes;

[Prototype("vessel")]
public sealed class VesselPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Vessel name.
    /// </summary>
    [DataField("name")] public string Name = string.Empty;

    /// <summary>
    ///     Short description of the vessel.
    /// </summary>
    [DataField("description")] public string Description = string.Empty;

    /// <summary>
    ///     The price of the vessel
    /// </summary>
    [DataField("price", required: true)]
    public int Price;

    /// <summary>
    ///     The category of the product. (e.g. Small, Medium, Large, Emergency, Special etc.)
    /// </summary>
    [DataField("category", required: true)]
    public VesselSize Category = VesselSize.Small;

    /// <summary>
    ///     The group of the product. (e.g. Civilian, Syndicate, Contraband etc.)
    /// </summary>
    [DataField("group", required: true)]
    public ShipyardConsoleUiKey Group = ShipyardConsoleUiKey.Shipyard;

    /// <summary>
    ///     The group of the product. (e.g. Civilian, Syndicate, Contraband etc.)
    /// </summary>
    [DataField("class")]
    public List<VesselClass> Classes = new();

    /// <summary>
    ///     The access required to buy the product. (e.g. Command, Mail, Bailiff, etc.)
    /// </summary>
    [DataField("access")]
    public string Access = string.Empty;

    /// Frontier - Add this field for the MapChecker script.
    /// <summary>
    ///     The MapChecker override group for this vessel.
    /// </summary>
    [DataField("mapchecker_group_override")]
    public string MapcheckerGroup = string.Empty;

    /// <summary>
    ///     Relative directory path to the given shuttle, i.e. `/Maps/Shuttles/yourshittle.yml`
    /// </summary>
    [DataField("shuttlePath", required: true)]
    public ResPath ShuttlePath = default!;

    /// <summary>
    ///     Grid protections for a given ship. Should be None in _most_ cases.
    /// </summary>
    [DataField("gridProtection")]
    public GridProtectionFlags GridProtection = GridProtectionFlags.None;

    /// <summary>
    ///     Guidebook page associated with a shuttle
    /// </summary>
    [DataField]
    public ProtoId<GuideEntryPrototype>? GuidebookPage = default!;
}

public enum VesselSize : byte
{
    All, // Should not be used by ships, intended as a placeholder value to represent everything
    Micro,
    Small,
    Medium,
    Large
}

public enum VesselClass : byte
{
    All, // Should not be used by ships, intended as a placeholder value to represent everything
    // Capabilities
    Expedition,
    Scrapyard,
    // General
    Salvage,
    Science,
    Cargo,
    Chemistry,
    Botany,
    Engineering,
    Atmospherics,
    Mercenary,
    Medical,
    Civilian, // Service catch-all - reporter, legal, entertainment, misc. ships
    Kitchen,
    // Antag ships
    Syndicate,
    Pirate,
    // NFSD-specific categories
    Detainment,
    Detective,
    Fighter,
    Stealth,
    Capital,
}


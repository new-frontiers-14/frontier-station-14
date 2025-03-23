using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Shipyard.Prototypes;

[Prototype]
public sealed class VesselPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<VesselPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     Vessel name.
    /// </summary>
    [DataField] public string Name = string.Empty;

    /// <summary>
    ///     Short description of the vessel.
    /// </summary>
    [DataField] public string Description = string.Empty;

    /// <summary>
    ///     The price of the vessel
    /// </summary>
    [DataField(required: true)]
    public int Price;

    /// <summary>
    ///     The size of the vessel. (e.g. Small, Medium, Large etc.)
    /// </summary>
    [DataField(required: true)]
    public VesselSize Category = VesselSize.Small;

    /// <summary>
    ///     The shipyard listing that the vessel should be in. (e.g. Civilian, Syndicate, Contraband etc.)
    /// </summary>
    [DataField(required: true)]
    public ShipyardConsoleUiKey Group = ShipyardConsoleUiKey.Shipyard;

    /// <summary>
    ///     The purpose of the vessel. (e.g. Service, Cargo, Engineering etc.)
    /// </summary>
    [DataField("class")]
    public List<VesselClass> Classes = new();

    /// <summary>
    ///     The engine type that powers the vessel. (e.g. AME, Plasma, Solar etc.)
    /// </summary>
    [DataField("engine")]
    public List<VesselEngine> Engines = new();

    /// <summary>
    ///     The access required to buy the product. (e.g. Command, Mail, Bailiff, etc.)
    /// </summary>
    [DataField]
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
    [DataField(required: true)]
    public ResPath ShuttlePath = default!;

    /// <summary>
    ///     Guidebook page associated with a shuttle
    /// </summary>
    [DataField]
    public ProtoId<GuideEntryPrototype>? GuidebookPage = default!;

    /// <summary>
    ///     The price markup of the vessel testing
    /// </summary>
    [DataField]
    public float MinPriceMarkup = 1.05f;

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry AddComponents { get; set; } = new();
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
    // NFSD-specific categories
    Capital,
    Detainment,
    Detective,
    Fighter,
    Patrol,
    Pursuit,
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
}

public enum VesselEngine : byte
{
    All, // Should not be used by ships, intended as a placeholder value to represent everything
    AME,
    TEG,
    Supermatter,
    Tesla,
    Singularity,
    Solar,
    RTG,
    APU,
    Welding,
    Plasma,
    Uranium,
    Bananium,
}

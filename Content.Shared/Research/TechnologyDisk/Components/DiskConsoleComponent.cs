using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsoleComponent : Component
{

    public static string TargetIdCardSlotId = "DiskConsole-targetId";
    public static string TargetBundleDiskSlotId = "DiskConsole-targetBundleDisk";

    /// <summary>
    /// The slot where the target ID card is stored
    /// </summary>
    [DataField("targetIdSlot")]
    public ItemSlot TargetIdSlot = new();

    /// <summary>
    /// The slot where the bundle disk is stored.
    /// </summary>
    [DataField("targetBundleDiskSlot")]
    public ItemSlot TargetBundleDiskSlot = new();

    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    /// <summary>
    /// How much it costs to print a disk
    /// </summary>
    [DataField("pricePerDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePerDisk = 10000;

    /// <summary>
    /// How much it costs to print a rare disk
    /// </summary>
    [DataField("pricePerRareDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePerRareDisk = 13000;

    /// <summary>
    /// The prototype of what's being printed
    /// </summary>
    [DataField("diskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string DiskPrototype = "TechnologyDisk";

    [DataField("diskPrototypeRare", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string DiskPrototypeRare = "TechnologyDiskRare";

    [DataField("diskPrototypeBundled", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string DiskPrototypeBundled = "TechnologyDiskBundled";

    [DataField("diskRare"), ViewVariables(VVAccess.ReadWrite)]
    public bool DiskRare = false;

    [DataField("diskAllResearch"), ViewVariables(VVAccess.ReadWrite)]
    public bool DiskAllResearch = false;

    /// <summary>
    /// How long it takes to print <see cref="DiskPrototype"/>
    /// </summary>
    [DataField("printDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}

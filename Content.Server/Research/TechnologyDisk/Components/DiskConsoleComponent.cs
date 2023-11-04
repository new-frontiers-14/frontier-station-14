using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsoleComponent : Component
{
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

    [DataField("diskRare"), ViewVariables(VVAccess.ReadWrite)]
    public bool DiskRare = false;

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
}

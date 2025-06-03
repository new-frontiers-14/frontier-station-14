using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cloning;

[RegisterComponent]
public sealed partial class CloningPodComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> PodPort = "CloningPodReceiver";

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// How long the cloning has been going on for.
    /// </summary>
    [ViewVariables]
    public float CloningProgress = 0;

    [ViewVariables]
    public int UsedBiomass = 70;

    [ViewVariables]
    public bool FailedClone = false;

    /// <summary>
    /// The material that is used to clone entities.
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Biomass";

    /// <summary>
    /// The current amount of time it takes to clone a body.
    /// </summary>
    [DataField]
    public float CloningTime = 30f;

    /// <summary>
    /// The mob to spawn on emag.
    /// </summary>
    [DataField]
    public EntProtoId MobSpawnId = "MobAbomination";

    /// <summary>
    /// The sound played when a mob is spawned from an emagged cloning pod.
    /// </summary>
    [DataField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("ZombieScreams")
    {
        Params = AudioParams.Default.WithVolume(4),
    };

    /// <summary>
    /// The machine part that affects how much biomass is needed to clone a body.
    /// </summary>
    [DataField("partRatingMaterialMultiplier")]
    public float PartRatingMaterialMultiplier = 0.85f;

    // Frontier: machine part upgrades
    /// <summary>
    /// The base multiplier on the body weight, which determines the
    /// amount of biomass needed to clone, and is affected by part upgrades.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseBiomassRequirementMultiplier = 1;

    // Frontier: machine part upgrades
    /// <summary>
    /// The current multiplier on the body weight, which determines the
    /// amount of biomass needed to clone.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BiomassRequirementMultiplier = 1;

    /// <summary>
    /// The machine part that decreases the amount of material needed for cloning
    /// </summary>
    [DataField("machinePartMaterialUse"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MachinePartPrototype> MachinePartMaterialUse = "MatterBin";

    [ViewVariables(VVAccess.ReadWrite)]
    public CloningPodStatus Status;

    [ViewVariables]
    public EntityUid? ConnectedConsole;

    // Frontier: macihine upgrades
    /// <summary>
    /// The base amount of time it takes to clone a body
    /// </summary>
    [DataField]
    public float BaseCloningTime = 30f;

    /// <summary>
    /// The multiplier for cloning duration
    /// </summary>
    [DataField]
    public float PartRatingSpeedMultiplier = 0.75f;

    /// <summary>
    /// The machine part that affects cloning speed
    /// </summary>
    [DataField]
    public ProtoId<MachinePartPrototype> MachinePartCloningSpeed = "Manipulator";
    // End Frontier: machine upgrades
}

[Serializable, NetSerializable]
public enum CloningPodVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum CloningPodStatus : byte
{
    Idle,
    Cloning,
    Gore,
    NoMind
}

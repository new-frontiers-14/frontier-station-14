using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.Expeditions;

[Serializable, NetSerializable]
public sealed class SalvageExpeditionConsoleState : BoundUserInterfaceState
{
    public TimeSpan NextOffer;
    public bool Claimed;
    public bool Cooldown;
    public bool CanFinish; // Frontier
    public ushort ActiveMission;
    public List<SalvageMissionParams> Missions;

    public SalvageExpeditionConsoleState(TimeSpan nextOffer, bool claimed, bool cooldown, bool canFinish, ushort activeMission, List<SalvageMissionParams> missions)
    {
        NextOffer = nextOffer;
        Claimed = claimed;
        Cooldown = cooldown;
        CanFinish = canFinish; // Frontier
        ActiveMission = activeMission;
        Missions = missions;
    }
}

/// <summary>
/// Used to interact with salvage expeditions and claim them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageExpeditionConsoleComponent : Component
{
    /// <summary>
    /// The sound made when spawning a coordinates disk
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/terminal_insert_disc.ogg");

    /// <summary>
    /// Frontier: Adding error to the FTL warning - Hard to tell without it - PR 377
    /// </summary>
    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
    new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    /// <summary>
    /// Frontier: Debug mod
    /// </summary>
    [DataField]
    public bool Debug = false;
}

[Serializable, NetSerializable]
public sealed class ClaimSalvageMessage : BoundUserInterfaceMessage
{
    public ushort Index;
}

[Serializable, NetSerializable] // Frontier
public sealed class FinishSalvageMessage : BoundUserInterfaceMessage;

/// <summary>
/// Added per station to store data on their available salvage missions.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SalvageExpeditionDataComponent : Component
{
    /// <summary>
    /// Is there an active salvage expedition.
    /// </summary>
    [ViewVariables]
    public bool Claimed => ActiveMission != 0;

    /// <summary>
    /// Are we actively cooling down from the last salvage mission.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public bool Cooldown = false;

    /// <summary>
    /// Frontier - Allow early finish.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool CanFinish = false;

    /// <summary>
    /// Nexy time salvage missions are offered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextOffer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextOffer;

    [ViewVariables]
    public readonly Dictionary<ushort, SalvageMissionParams> Missions = new();

    [ViewVariables] public ushort ActiveMission;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed record SalvageMissionParams : IComparable<SalvageMissionParams>
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables(VVAccess.ReadWrite)]
    public SalvageMissionType MissionType;

    [ViewVariables(VVAccess.ReadWrite)] public int Seed;

    /// <summary>
    /// Base difficulty for this mission.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public DifficultyRating Difficulty;

    public int CompareTo(SalvageMissionParams? other)
    {
        if (other == null)
            return -1;

        return Difficulty.CompareTo(other.Difficulty);
    }
}

/// <summary>
/// Created from <see cref="SalvageMissionParams"/>. Only needed for data the client also needs for mission
/// display.
/// </summary>
public sealed record SalvageMission(
    int Seed,
    DifficultyRating Difficulty,
    string Dungeon,
    string Faction,
    SalvageMissionType Mission,
    string Biome,
    string Air,
    float Temperature,
    Color? Color,
    TimeSpan Duration,
    List<string> Rewards,
    List<string> Modifiers)
{
    /// <summary>
    /// Seed used for the mission.
    /// </summary>
    public readonly int Seed = Seed;

    /// <summary>
    /// Difficulty rating.
    /// </summary>
    public DifficultyRating Difficulty = Difficulty;

    /// <summary>
    /// <see cref="SalvageDungeonMod"/> to be used.
    /// </summary>
    public readonly string Dungeon = Dungeon;

    /// <summary>
    /// <see cref="SalvageFactionPrototype"/> to be used.
    /// </summary>
    public readonly string Faction = Faction;

    /// <summary>
    /// Underlying mission params that generated this.
    /// </summary>
    public readonly SalvageMissionType Mission = Mission;

    /// <summary>
    /// Biome to be used for the mission.
    /// </summary>
    public readonly string Biome = Biome;

    /// <summary>
    /// Air mixture to be used for the mission's planet.
    /// </summary>
    public readonly string Air = Air;

    /// <summary>
    /// Temperature of the planet's atmosphere.
    /// </summary>
    public readonly float Temperature = Temperature;

    /// <summary>
    /// Lighting color to be used (AKA outdoor lighting).
    /// </summary>
    public readonly Color? Color = Color;

    /// <summary>
    /// Mission duration.
    /// </summary>
    public TimeSpan Duration = Duration;

    /// <summary>
    /// The list of items to order on mission completion.
    /// </summary>
    public List<string> Rewards = Rewards;

    /// <summary>
    /// Modifiers (outside of the above) applied to the mission.
    /// </summary>
    public List<string> Modifiers = Modifiers;
}

[Serializable, NetSerializable]
public enum SalvageConsoleUiKey : byte
{
    Expedition,
}

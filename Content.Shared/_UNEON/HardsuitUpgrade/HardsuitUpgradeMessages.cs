using Robust.Shared.Serialization;

namespace Content.Shared._UNEON.HardsuitUpgrade;

[Serializable, NetSerializable]
public enum HardsuitUpgradeUiKey : byte { Key }

[Serializable, NetSerializable]
public enum HardsuitUpgradeVisuals : byte { Working }

[Serializable, NetSerializable]
public enum HardsuitUpgradeKind : byte { Blunt, Slash, Piercing, Heat, Radiation, Caustic, Explosion, Speed }

[Serializable, NetSerializable]
public sealed class HardsuitUpgradeStartMessage(HardsuitUpgradeKind kind) : BoundUserInterfaceMessage
{
    public readonly HardsuitUpgradeKind Kind = kind;
}

[Serializable, NetSerializable]
public sealed class HardsuitUpgradeEjectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class HardsuitUpgradeMaterialsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class HardsuitUpgradeOption(HardsuitUpgradeKind kind, float current, float target, bool affordable, bool available)
{
    public readonly HardsuitUpgradeKind Kind = kind;
    public readonly float Current = current;
    public readonly float Target = target;
    public readonly bool Affordable = affordable;
    public readonly bool Available = available;
}

[Serializable, NetSerializable]
public sealed class HardsuitUpgradeState(NetEntity? suit, string? suitName, bool powered, bool busy, float remaining, float duration,
    List<HardsuitUpgradeOption> options, Dictionary<string, int> materials, bool hasLocalMaterials,
    bool siloLinked, bool siloAvailable) : BoundUserInterfaceState
{
    public readonly NetEntity? Suit = suit;
    public readonly string? SuitName = suitName;
    public readonly bool Powered = powered;
    public readonly bool Busy = busy;
    public readonly float Remaining = remaining;
    public readonly float Duration = duration;
    public readonly List<HardsuitUpgradeOption> Options = options;
    public readonly Dictionary<string, int> Materials = materials;
    public readonly bool HasLocalMaterials = hasLocalMaterials;
    public readonly bool SiloLinked = siloLinked;
    public readonly bool SiloAvailable = siloAvailable;
}

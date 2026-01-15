using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.CollectiveMind;

[RegisterComponent, NetworkedComponent]
public sealed partial class CollectiveMindComponent : Component
{
    [DataField("minds")]
    public Dictionary<CollectiveMindPrototype, CollectiveMindMemberData> Minds = new();

    [DataField]
    public bool BlockWhenUnconscious = false;

    [DataField]
    public bool CorruptWhenUnconscious = true;

    [DataField]
    public float CorruptionChanceWhenUnconscious = 0.85f;
}

/// <summary>
/// Stores data about the collective mind member.
/// </summary>
[Serializable]
public sealed class CollectiveMindMemberData
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int MindId = 1; // this value determines the starting mind id for members of the collective mind.
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CollectiveMindIdentityComponent : Component
{
    [DataField]
    public string PrototypeId;

    [DataField]
    public CollectiveMindMemberData? MindData;
}

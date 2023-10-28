using Content.Server.NPC.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Prototypes;

namespace Content.Server._Park.Species.Shadowkin.Components;

[RegisterComponent]
public sealed partial class ShadowkinDarkSwapPowerComponent : Component
{
    [DataField]
    public EntProtoId DarkSwapAction = "ShadowkinDarkSwap";

    [DataField("DarkSwapActionEntity"), AutoNetworkedField]
    public EntityUid? DarkSwapActionEntity;

    /// <summary>
    ///     Factions temporarily deleted from the entity while swapped
    /// </summary>
    public List<string> SuppressedFactions = new();

    /// <summary>
    ///     Factions temporarily added to the entity while swapped
    /// </summary>
    [DataField("factions", customTypeSerializer: typeof(PrototypeIdListSerializer<NpcFactionPrototype>))]
    public List<string> AddedFactions = new() { "ShadowkinDarkFriendly" };
}

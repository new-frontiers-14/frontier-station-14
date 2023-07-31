using System.Threading;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.SpaceArtillery;

[RegisterComponent, NetworkedComponent]
public sealed class SpaceArtilleryComponent : Component
{
	
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField("firePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string FirePort = "Fire";
	
}
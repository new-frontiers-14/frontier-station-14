using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Server.Abilities.Felinid
{
    [RegisterComponent]
    public sealed class FelinidComponent : Component
    {
        /// <summary>
        /// The hairball prototype to use.
        /// </summary>
        [DataField("hairballPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string HairballPrototype = "Hairball";

        [DataField("hairballAction")]
        public InstantAction? HairballAction;

        public EntityUid? PotentialTarget = null;
    }
}

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.Utility;

namespace Content.Server.Abilities.Felinid
{
    [RegisterComponent]
    public sealed partial class FelinidComponent : Component
    {
        /// <summary>
        /// The hairball prototype to use.
        /// </summary>
        [DataField("hairballPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string HairballPrototype = "Hairball";

        [DataField("hairballActionPrototype")]
        public string HairballActionPrototype = "ActionHairBall";

        public EntityUid? HairballAction = null;
        public EntityUid? EatMouseAction = null;
        public EntityUid? PotentialTarget = null;
    }
}

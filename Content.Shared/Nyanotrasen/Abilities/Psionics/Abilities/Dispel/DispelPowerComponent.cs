using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class DispelPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 10f;

        [DataField("dispelActionId",
            customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? DispelActionId = "ActionDispel";

        [DataField("dispelActionEntity")]
        public EntityUid? DispelActionEntity;
    }
}

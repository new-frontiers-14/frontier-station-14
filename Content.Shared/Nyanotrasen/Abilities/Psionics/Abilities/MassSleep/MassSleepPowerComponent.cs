using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MassSleepPowerComponent : Component
    {
        public float Radius = 1.25f;
        [DataField("massSleepActionId",
            customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? MassSleepActionId = "ActionTelegnosis";

        [DataField("massSleepActionEntity")]
        public EntityUid? MassSleepActionEntity;
    }
}

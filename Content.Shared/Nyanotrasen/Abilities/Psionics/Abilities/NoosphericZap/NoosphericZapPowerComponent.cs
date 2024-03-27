using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class NoosphericZapPowerComponent : Component
    {
        [DataField("noosphericZapActionId",
            customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? NoosphericZapActionId = "ActionNoosphericZap";

        [DataField("noosphericZapActionEntity")]
        public EntityUid? NoosphericZapActionEntity;
    }
}

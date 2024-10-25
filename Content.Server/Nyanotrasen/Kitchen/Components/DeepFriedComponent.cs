using Content.Shared.Nyanotrasen.Kitchen.Components;
using Content.Shared.Nyanotrasen.Kitchen.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Nyanotrasen.Kitchen.Components
{
    [RegisterComponent]
    //This line appears to be deprecated. [ComponentReference(typeof(SharedDeepFriedComponent))]
    public sealed partial class DeepFriedComponent : SharedDeepFriedComponent
    {
        /// <summary>
        /// What is the item's base price multiplied by?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("priceCoefficient")]
        public float PriceCoefficient { get; set; } = 1.0f;

        /// <summary>
        /// What was the entity's original name before any modification?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("originalName")]
        public string? OriginalName { get; set; }

        /// <summary>
        /// Frontier: the crispiness level set to use for shaders, examination, etc.
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CrispinessLevelSetPrototype>))]
        [AutoNetworkedField]
        public string CrispinessLevelSet { get; set; } = "Crispy";
    }
}

using Content.Shared.Nyanotrasen.Kitchen.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Nyanotrasen.Kitchen.Components
{
    [NetworkedComponent]
    public abstract partial class SharedDeepFriedComponent : Component
    {
        /// <summary>
        /// How deep-fried is this item?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("crispiness")]
        public int Crispiness { get; set; }
    }

    [Serializable, NetSerializable]
    public enum DeepFriedVisuals : byte
    {
        Fried,
        Spectral, // Frontier
    }
}

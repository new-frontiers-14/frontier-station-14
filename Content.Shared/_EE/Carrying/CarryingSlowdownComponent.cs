using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Carrying
{
    [RegisterComponent, NetworkedComponent, Access(typeof(CarryingSlowdownSystem))]

    public sealed partial class CarryingSlowdownComponent : Component
    {
        [DataField(required: true)]
        public float WalkModifier = 1.0f;

        [DataField(required: true)]
        public float SprintModifier = 1.0f;
    }

    [Serializable, NetSerializable]
    public sealed class CarryingSlowdownComponentState : ComponentState
    {
        public float WalkModifier;
        public float SprintModifier;
        public CarryingSlowdownComponentState(float walkModifier, float sprintModifier)
        {
            WalkModifier = walkModifier;
            SprintModifier = sprintModifier;
        }
    }
}

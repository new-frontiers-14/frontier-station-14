using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Ghost
{
    [NetworkedComponent]
    [AutoGenerateComponentState]
    public abstract partial class SharedGhostComponent : Component
    {
        // TODO: instead of this funny stuff just give it access and update in system dirtying when needed
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanGhostInteract
        {
            get => _canGhostInteract;
            set
            {
                if (_canGhostInteract == value) return;
                _canGhostInteract = value;
                Dirty();
            }
        }

        [DataField("canInteract"), AutoNetworkedField]
        private bool _canGhostInteract;

        /// <summary>
        ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
        /// </summary>
        // TODO MIRROR change this to use friend classes when thats merged
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                if (_canReturnToBody == value) return;
                _canReturnToBody = value;
                Dirty();
            }
        }

        /// <summary>
        /// Ghost color
        /// </summary>
        /// <remarks>Used to allow admins to change ghost colors. Should be removed if the capability to edit existing sprite colors is ever added back.</remarks>
        [DataField("color"), AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public Color color = Color.White;

        [DataField("canReturnToBody"), AutoNetworkedField]
        private bool _canReturnToBody;

        [DataField("TimeOfDeath", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan TimeOfDeath = TimeSpan.Zero;

        public override ComponentState GetComponentState()
        {
            return new GhostComponentState(CanReturnToBody, CanGhostInteract, TimeOfDeath);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
            CanGhostInteract = state.CanGhostInteract;
            TimeOfDeath = state.TimeOfDeath;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public bool CanGhostInteract { get; }

        public TimeSpan TimeOfDeath { get; }

        public GhostComponentState(
            bool canReturnToBody,
            bool canGhostInteract,
            TimeSpan timeOfDeath)
        {
            CanReturnToBody = canReturnToBody;
            CanGhostInteract = canGhostInteract;
            TimeOfDeath = timeOfDeath;
        }
    }
}



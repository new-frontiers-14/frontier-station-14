using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Bodycam
{
    [NetworkedComponent]
    [RegisterComponent]
    [Access(typeof(SharedBodycamSystem))]
    public sealed class BodycamComponent : Robust.Shared.GameObjects.Component
    {
        public bool Activated;

        [DataField("turnOnSound")]
        public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

        [DataField("turnOffSound")]
        public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

        /// <summary>
        ///     Whether to automatically set item-prefixes when toggling the flashlight.
        /// </summary>
        /// <remarks>
        ///     Flashlights should probably be using explicit unshaded sprite, in-hand and clothing layers, this is
        ///     mostly here for backwards compatibility.
        /// </remarks>
        [DataField("addPrefix")]
        public bool AddPrefix = false;

        [DataField("toggleActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ToggleActionId = "ToggleBodycam";

        [DataField("toggleAction")]
        public InstantAction? ToggleAction;

        public const int StatusLevels = 6;

        [Serializable, NetSerializable]
        public sealed class BodycamComponentState : ComponentState
        {
            public bool Activated { get; }

            public BodycamComponentState(bool activated, byte? charge)
            {
                Activated = activated;
            }
        }
    }
}

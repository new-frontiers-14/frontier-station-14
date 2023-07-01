using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedStunSystem))]
    public sealed class KnockedDownComponent : Component
    {
        [DataField("helpInterval")]
        public float HelpInterval { get; set; } = 1f;

        [DataField("helpAttemptSound")]
        public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [ViewVariables]
        public TimeSpan? NextHelp;
    }

    [Serializable, NetSerializable]
    public sealed class KnockedDownComponentState : ComponentState
    {
        public float HelpInterval { get; set; }
        public TimeSpan? NextHelp { get; set; }

        public KnockedDownComponentState(float helpInterval, TimeSpan? nextHelp)
        {
            HelpInterval = helpInterval;
            NextHelp = nextHelp;
        }
    }
}

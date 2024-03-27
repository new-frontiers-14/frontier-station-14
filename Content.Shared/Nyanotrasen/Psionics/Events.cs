using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.Psionics.Events
{
    [Serializable, NetSerializable]
    public sealed partial class PsionicRegenerationDoAfterEvent : DoAfterEvent
    {
        [DataField("startedAt", required: true)]
        public TimeSpan StartedAt;

        private PsionicRegenerationDoAfterEvent()
        {
        }

        public PsionicRegenerationDoAfterEvent(TimeSpan startedAt)
        {
            StartedAt = startedAt;
        }

        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class GlimmerWispDrainDoAfterEvent : SimpleDoAfterEvent
    {
    }
}

using Robust.Shared.Audio;

namespace Content.Server.Abilities.Gachi.Components
{
    [RegisterComponent]
    public sealed partial class GachiComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("painSound")]
        public SoundSpecifier PainSound { get; set; } = new SoundCollectionSpecifier("GachiPain");

        [DataField("hitOtherSound")]
        public SoundSpecifier HitOtherSound { get; set; } = new SoundCollectionSpecifier("GachiHitOther");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("multiplier")]
        public float Multiplier = 1f;

        public float Accumulator = 0f;

        public TimeSpan AddToMultiplierTime = TimeSpan.FromSeconds(1);
    }
}

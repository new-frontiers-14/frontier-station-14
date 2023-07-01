namespace Content.Server.Abilities.Felinid
{
    [RegisterComponent]
    public sealed class CoughingUpHairballComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("coughUpTime")]
        public TimeSpan CoughUpTime = TimeSpan.FromSeconds(2.15); // length of hairball.ogg
    }
}

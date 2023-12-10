namespace Content.Server.Psionics.Glimmer
{
    [RegisterComponent]
    /// <summary>
    /// Adds to glimmer at regular intervals. We'll use it for glimmer drains too when we get there.
    /// </summary>
    public sealed class GlimmerSourceComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("active")]
        public bool Active = true;

        /// <summary>
        ///     Since glimmer is an int, we'll do it like this.
        /// </summary>
        [DataField("secondsPerGlimmer")]
        public float SecondsPerGlimmer = 10f;

        /// <summary>
        ///     True if it produces glimmer, false if it subtracts it.
        /// </summary>
        [DataField("addToGlimmer")]
        public bool AddToGlimmer = true;
    }
}

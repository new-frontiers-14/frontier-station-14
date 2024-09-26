namespace Content.Server.Carrying
{
    /// <summary>
    /// Added to an entity when they are carrying somebody.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CarryingComponent : Component
    {
        public EntityUid Carried = default!;

        // Frontier: throwing parameters
        /// <summary>
        ///     A base coefficient to multiply throwing speed by.
        /// </summary>
        [DataField]
        public float BaseThrowingSpeedCoeff = 0.5f;

        /// <summary>
        ///     A maximum contest value for throwing speed.
        /// </summary>
        [DataField]
        public float MaxContestThrowingSpeedCoeff = 2.0f;
        // End Frontier: throwing parameters
    }
}

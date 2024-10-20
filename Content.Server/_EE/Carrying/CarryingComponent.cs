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
        ///     The maximum coefficient to multiply throwing speed by, regardless of contest parameters.
        ///     Proportional to "the fastest this entity can lob a person"
        /// </summary>
        [DataField]
        public float MaxThrowingSpeedCoeff = 1.0f;
        // End Frontier: throwing parameters
    }
}

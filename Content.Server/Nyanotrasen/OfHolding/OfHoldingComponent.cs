namespace Content.Server.OfHolding
{
    [RegisterComponent]
    public sealed partial class OfHoldingComponent : Component
    {
        /// <summary>
        /// The last entity this bag warned,
        /// used to warn people first.
        /// </summary>
        public EntityUid? LastWarnedEntity = null;
    }
}

using System.Threading;

namespace Content.Server.Carrying
{
    [RegisterComponent]
    public sealed partial class CarriableComponent : Component
    {
        /// <summary>
        ///     Number of free hands required
        ///     to carry the entity
        /// </summary>
        [DataField]
        public int FreeHandsRequired = 2;

        public CancellationTokenSource? CancelToken;

        /// <summary>
        ///     The base duration (In Seconds) of how long it should take to pick up this entity
        ///     before Contests are considered.
        /// </summary>
        [DataField]
        public float PickupDuration = 3;

        // Frontier: min/max sanitization
        /// <summary>
        ///     The minimum duration (in seconds) of how long it should take to pick up this entity.
        ///     When the heaviest object picks up lightest object, it should roughly take this long.
        /// </summary>
        [DataField]
        public float MinPickupDuration = 1.5f;

        /// <summary>
        ///     The maximum duration (in seconds) of how long it should take to pick up this entity.
        ///     When an object picks up the heaviest object it can lift, it should be under this.
        /// </summary>
        [DataField]
        public float MaxPickupDuration = 6.0f;
        // End Frontier
    }
}

using System.Threading;
using Content.Shared._NF.Trade;

namespace Content.Server._NF.Trade;

/// <summary>
/// This is used to mark an entity to be used as a trade crate
/// </summary>
[RegisterComponent]
public sealed partial class TradeCrateComponent : SharedTradeCrateComponent
{
    /// <summary>
    /// The value of the crate, in spesos, when at its destination.
    /// </summary>
    [DataField]
    public int ValueAtDestination;

    /// <summary>
    /// The value of the crate, in spesos, when delivered elsewhere.
    /// </summary>
    [DataField]
    public int ValueElsewhere;

    /// <summary>
    /// If non-zero, this crate will be an express delivery.
    /// </summary>
    [DataField]
    public TimeSpan ExpressDeliveryDuration = TimeSpan.Zero;

    /// <summary>
    /// If non-null, the package must be redeemed before this time to arrive unpenalized.
    /// </summary>
    [ViewVariables]
    public TimeSpan? ExpressDeliveryTime;
    /// <summary>
    /// The bonus this package will receive if delivered on-time.
    /// </summary>
    [DataField]
    public int ExpressOnTimeBonus;
    /// <summary>
    /// The penalty this package will receive if delivered late.
    /// </summary>
    [DataField]
    public int ExpressLatePenalty;

    /// <summary>
    /// This crate's destination.
    /// </summary>
    [ViewVariables]
    public EntityUid DestinationStation;

    /// <summary>
    /// Cancellation token used to disable the priority marker on
    /// </summary>
    public CancellationTokenSource? PriorityCancelToken;
}

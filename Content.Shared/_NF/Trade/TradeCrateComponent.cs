using System.Threading;
using Content.Shared.Cargo;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Trade;

/// <summary>
/// This is used to mark an entity to be used as a trade crate
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, Access(typeof(SharedCargoSystem))]
public sealed partial class TradeCrateComponent : Component
{
    /// <summary>
    /// The value of the crate, in spesos, when delivered to its destination.
    /// </summary>
    [DataField(serverOnly: true)]
    public int ValueAtDestination;

    /// <summary>
    /// The value of the crate, in spesos, when delivered elsewhere.
    /// </summary>
    [DataField(serverOnly: true)]
    public int ValueElsewhere;

    /// <summary>
    /// If non-zero, this crate will be an express delivery.
    /// </summary>
    [DataField(serverOnly: true)]
    public TimeSpan ExpressDeliveryDuration = TimeSpan.Zero;

    /// <summary>
    /// If non-null, the package must be redeemed before this time to arrive unpenalized.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? ExpressDeliveryTime;

    /// <summary>
    /// The bonus this package will receive if delivered on-time.
    /// </summary>
    [DataField(serverOnly: true)]
    public int ExpressOnTimeBonus;

    /// <summary>
    /// The penalty this package will receive if delivered late.
    /// </summary>
    [DataField(serverOnly: true)]
    public int ExpressLatePenalty;

    /// <summary>
    /// This crate's destination.
    /// </summary>
    [ViewVariables]
    public EntityUid DestinationStation;

    /// <summary>
    /// Cancellation token used to disable the express marker on the crate.
    /// </summary>
    public CancellationTokenSource? ExpressCancelToken;
}

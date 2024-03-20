using Content.Shared._NF.Cargo;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Cargo.Components;

/// <summary>
/// Stores all of cargo orders for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class FrontierStationCargoOrderDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of orders a station is allowed, approved or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 20;

    [ViewVariables(VVAccess.ReadWrite), DataField("orders")]
    public List<FrontierCargoOrderData> Orders = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    public int NumOrdersCreated;

    // TODO: Can probably dump this
    /// <summary>
    /// The cargo shuttle assigned to this station.
    /// </summary>
    [DataField("shuttle")]
    public EntityUid? Shuttle;

    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField]
    public EntProtoId PrinterOutput = "PaperCargoInvoice";
}

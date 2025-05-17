using Content.Shared._NF.Cargo;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Cargo.Components;

/// <summary>
/// Stores all of cargo orders for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class NFStationCargoOrderDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of orders a station is allowed, approved or not.
    /// </summary>
    [DataField]
    public int Capacity = 20;

    [DataField]
    public List<NFCargoOrderData> Orders = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [ViewVariables]
    public int NumOrdersCreated;

    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField]
    public EntProtoId PrinterOutput = "PaperCargoInvoice";
}

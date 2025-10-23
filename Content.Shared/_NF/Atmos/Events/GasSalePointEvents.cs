using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Events;

/// <summary>
/// Raised on a client requesting gas to be sold.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasSaleSellMessage : BoundUserInterfaceMessage;

/// <summary>
/// Raised on a client requesting the gas console's state be refreshed
/// Similar to the appraise button on the cargo consoles.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasSaleRefreshMessage : BoundUserInterfaceMessage;

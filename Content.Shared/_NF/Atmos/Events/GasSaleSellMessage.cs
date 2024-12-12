using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Events;

/// <summary>
/// Raised on a client requesting gas to be sold.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasSaleSellMessage : BoundUserInterfaceMessage;

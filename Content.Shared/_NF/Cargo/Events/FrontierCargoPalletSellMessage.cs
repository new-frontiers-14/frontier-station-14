using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.Events;

/// <summary>
/// Raised on a client request pallet sale
/// </summary>
[Serializable, NetSerializable]
public sealed class FrontierCargoPalletSellMessage : BoundUserInterfaceMessage;

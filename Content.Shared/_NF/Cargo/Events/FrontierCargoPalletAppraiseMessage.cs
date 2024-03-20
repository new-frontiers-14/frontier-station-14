using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.Events;

/// <summary>
/// Raised on a client request to refresh the pallet console
/// </summary>
[Serializable, NetSerializable]
public sealed class FrontierCargoPalletAppraiseMessage : BoundUserInterfaceMessage;

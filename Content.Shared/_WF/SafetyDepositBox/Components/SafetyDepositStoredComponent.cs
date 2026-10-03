using Robust.Shared.GameStates;

namespace Content.Shared._WF.SafetyDepositBox.Components;

/// <summary>
/// Marks an item as having been stored in a safety deposit box at some point.
/// Shows up in examine text.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SafetyDepositStoredComponent : Component
{
}

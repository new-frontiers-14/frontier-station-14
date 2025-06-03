using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._EE.Strip.Components;

/// <summary>
///     An item with this component is always hidden in the strip menu, regardless of other circumstances.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StripMenuHiddenComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared._DV.Whitelist;

/// <summary>
/// Marker component for any electronics whitelisting without having to copy paste infinite tags.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ElectronicsComponent : Component;

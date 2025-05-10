using Robust.Shared.GameStates;

namespace Content.Shared.Clothing._NF.Components;

/// <summary>
///     Auto toggle outer clothing when starting gear is equipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoToggleableOuterClothingComponent : Component;

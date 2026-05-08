using Robust.Shared.Containers;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Extends upstream's ToggleableClothingComponent.
///
///     This portion of the ToggleableClothingComponent stores the clothing item under the toggled piece.
///     Currently only supports a single piece of clothing, but pretty much all entities with ToggleableClothing
///     are just hardsuit helmets.
/// </summary>
public sealed partial class ToggleableClothingComponent : Component
{
    public const string DefaultUnderneathClothingContainerId = "toggleable-under-clothing";

    /// <summary>
    ///     The container ID of <see cref="UnderClothingContainer"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string UnderClothingContainerId = DefaultUnderneathClothingContainerId;

    /// <summary>
    ///     The container where the item that the toggled clothing replaced is put.
    /// </summary>
    [ViewVariables]
    public ContainerSlot? UnderClothingContainer;
}

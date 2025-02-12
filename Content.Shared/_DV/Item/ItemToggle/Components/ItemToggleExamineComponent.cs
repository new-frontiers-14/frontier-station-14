using Content.Shared._DV.Item.ItemToggle.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._DV.Item.ItemToggle.Components;

/// <summary>
/// Adds examine text when the item is on or off.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemToggleExamineSystem))]
public sealed partial class ItemToggleExamineComponent : Component
{
    [DataField(required: true)]
    public LocId On;

    [DataField(required: true)]
    public LocId Off;
}

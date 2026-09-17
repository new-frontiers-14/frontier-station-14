namespace Content.Shared._NF.Corgi.Components;

/// <summary>
/// Marker for clothing prototypes that smart corgis can equip.
/// Components inherit down the prototype tree but can't be removed by a
/// child. Use DisableCorgiWearableComponent on a child that shouldn't
/// inherit this from its parent.
/// </summary>
[RegisterComponent]
public sealed partial class CorgiWearableComponent : Component
{
}

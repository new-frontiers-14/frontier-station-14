namespace Content.Shared._NF.Corgi.Components;

/// <summary>
/// Blocks a CorgiWearableComponent inherited from a parent prototype.
/// Checked via the inventory slot blacklists in
/// smartcorgi_inventory_template.yml.
/// </summary>
[RegisterComponent]
public sealed partial class DisableCorgiWearableComponent : Component
{
}

namespace Content.Shared._NF.BindToStation;

/// <summary>
/// Denotes an entity that can be bound to a station.
/// Can be disabled in child entities to exempt from binding.
/// </summary>
[RegisterComponent]
public sealed partial class BindToStationComponent : Component
{
    /// <summary>
    /// If set to false, this will not be bound to a station.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}

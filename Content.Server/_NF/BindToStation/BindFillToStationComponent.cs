namespace Content.Server._NF.BindToStation;

/// <summary>
/// Any object with this will have its contents bound to the station it's on.
/// Nasty hack for storage and spawners initializing after the
/// </summary>
[RegisterComponent]
public sealed partial class BindFillToStationComponent : Component;

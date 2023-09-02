namespace Content.Server.Cargo.Components;

/// <summary>
/// This is used to mark an entity to be used in a trade crates
/// </summary>
[RegisterComponent]
public sealed class TradeCrateComponent : Component
{
    [DataField("item"), ViewVariables(VVAccess.ReadWrite)]
    public bool Item = false;

    [DataField("crate"), ViewVariables(VVAccess.ReadWrite)]
    public bool Crate = false;
}

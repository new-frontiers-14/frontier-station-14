[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class InflationCargoCrateComponent : Component
{
    [DataField("isInflated"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsInflated = false;
}

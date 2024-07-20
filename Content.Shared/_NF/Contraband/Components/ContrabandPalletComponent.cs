namespace Content.Shared._NF.Contraband.Components;

[RegisterComponent]
public sealed partial class ContrabandPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public string LocStringPrefix = string.Empty;
}

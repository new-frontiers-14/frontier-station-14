using Content.Server._NF.Cargo.Systems;
using Content.Shared.Whitelist;

namespace Content.Server._NF.Cargo.Components;

[RegisterComponent]
[Access(typeof(NFCargoSystem))]
public sealed partial class NFCargoPalletConsoleComponent : Component
{
    // The distance in a radius around the console to check for cargo pallets
    // Can be modified individually when mapping, so that consoles have a further reach
    [DataField]
    public int PalletDistance = 8;

    // The whitelist that determines what goods can be sold.  Accepts everything if null.
    [DataField]
    public EntityWhitelist? Whitelist;
}

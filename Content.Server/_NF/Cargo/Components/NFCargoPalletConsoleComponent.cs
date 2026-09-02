using Content.Server._NF.Cargo.Systems;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Cargo.Components;

[RegisterComponent]
[Access(typeof(NFCargoSystem))]
public sealed partial class NFCargoPalletConsoleComponent : Component
{
    /// <summary>
    /// The type of cash to spawn for anything being sold from the pallet.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";

    /// <summary>
    /// The distance in a radius around the console to check for cargo pallets.
    /// Can be modified individually when mapping, so that consoles have a further reach.
    /// </summary>
    [DataField]
    public int PalletDistance = 8;

    /// <summary>
    /// The whitelist that determines what goods can be sold.  Accepts everything if null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}

using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Whitelist; // Frontier

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    // Frontier
    // The distance in a radius around the console to check for cargo pallets
    // Can be modified individually when mapping, so that consoles have a further reach
    [DataField]
    public int PalletDistance = 8;

    // The whitelist that determines what goods can be sold.  Accepts everything if null.
    [DataField]
    public EntityWhitelist? Whitelist;
    // End Frontier
}

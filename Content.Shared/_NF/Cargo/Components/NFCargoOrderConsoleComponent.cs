using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._NF.Bank.Components;
using Content.Shared.Stacks;

namespace Content.Shared._NF.Cargo.Components;

/// <summary>
/// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedNFCargoSystem))]
public sealed partial class NFCargoOrderConsoleComponent : Component
{
    [DataField]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The stack representing cash dispensed on withdrawals.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";

    /// <summary>
    /// All of the <see cref="CargoProductPrototype.Group"/>s that are supported.
    /// </summary>
    [DataField]
    public List<string> AllowedGroups = new() { "market" };

    // Frontier: station taxes
    // Accounts to receive tax value (each currently receives the entirety of the taxed value)
    [DataField]
    public Dictionary<SectorBankAccount, float> TaxAccounts = new();
}

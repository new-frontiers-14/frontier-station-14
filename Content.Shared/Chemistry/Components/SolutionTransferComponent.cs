using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Audio; // Frontier

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Gives click behavior for transferring to/from other reagent containers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionTransferComponent : Component
{
    /// <summary>
    ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
    /// </summary>
    [DataField("transferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

    /// <summary>
    ///     The minimum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("minTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MinimumTransferAmount { get; set; } = FixedPoint2.New(5);

    /// <summary>
    ///     The maximum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("maxTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaximumTransferAmount { get; set; } = FixedPoint2.New(50);

    /// <summary>
    ///     Can this entity take reagent from reagent tanks?
    /// </summary>
    [DataField("canReceive")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanReceive { get; set; } = true;

    /// <summary>
    ///     Can this entity give reagent to other reagent containers?
    /// </summary>
    [DataField("canSend")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanSend { get; set; } = true;

    /// <summary>
    /// Whether you're allowed to change the transfer amount.
    /// </summary>
    [DataField("canChangeTransferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanChangeTransferAmount { get; set; } = false;

    /// <summary>
    ///     Frontier - Play a sound when transfering liquid
    /// </summary>
    [DataField("playTransferSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool PlayTransferSound { get; set; } = false;

    /// <summary>
    ///     Frontier - What sound to play when transfering liquid
    /// </summary>
    [DataField("transferSound")]
    public SoundSpecifier TransferSound =
    new SoundPathSpecifier("/Audio/_NF/Effects/splat.ogg");
}

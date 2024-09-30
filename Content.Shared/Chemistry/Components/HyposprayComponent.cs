using Content.Shared.DoAfter; // Frontier: Upstream, #30704 - MIT
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HyposprayComponent : Component
{
    [DataField]
    public string SolutionName = "hypospray";

    // TODO: This should be on clumsycomponent.
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ClumsyFailChance = 0.5f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Decides whether you can inject everything or just mobs.
    /// When you can only affect mobs, you're capable of drawing from beakers.
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    public bool OnlyAffectsMobs = false;

    /// <summary>
    /// Whether or not the hypospray is able to draw from containers or if it's a single use
    /// device that can only inject.
    /// </summary>
    [DataField]
    public bool InjectOnly = false;

    // Frontier: Upstream, #30704 - MIT
    /// <summary>
    /// If set over 0, enables a doafter for the hypospray which must be completed for injection.
    /// </summary>
    [DataField]
    public float DoAfterTime = 0f;
    // End Frontier

    /// <summary>
    /// Frontier: if true, object will not inject when attacking.
    /// </summary>
    [DataField]
    public bool PreventCombatInjection;
}

// Frontier: Upstream, #30704 - MIT
[Serializable, NetSerializable]
public sealed partial class HyposprayDoAfterEvent : SimpleDoAfterEvent
{
}
// End Frontier

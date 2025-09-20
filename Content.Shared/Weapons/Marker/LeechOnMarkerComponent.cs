using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Applies leech upon hitting a damage marker target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LeechOnMarkerComponent : Component
{
    // TODO: Can't network damagespecifiers yet last I checked.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("leech", required: true)]
    public DamageSpecifier Leech = new();

    // Frontier - Limited leech hits on dead mobs
    // The number of leech hits you can do on a mob until draining stops happening
    // Leeching always works if the foe is alive, but is checked against this when the mob is dead.
    [DataField] public int NumGuaranteedLeechHits = 5;
}

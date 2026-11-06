using Content.Shared.Body.Part;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Traits;

/// <summary>
/// Removes the use of a hand from the player.
/// Used for Unilateral Transradial Paralysis trait.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(OneHandParalyzedSystem))]
public sealed partial class OneHandParalyzedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? ParalyzedHand;
}

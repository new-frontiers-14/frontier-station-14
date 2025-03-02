using Robust.Shared.GameStates;

namespace Content.Shared._DV.Abilities.Felinid;

/// <summary>
/// Makes this food let felinids cough up a hairball when eaten.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedFelinidSystem))]
public sealed partial class FelinidFoodComponent : Component
{
    /// <summary>
    /// Extra hunger to satiate for felinids.
    /// </summary>
    [DataField]
    public float BonusHunger = 50f;
}

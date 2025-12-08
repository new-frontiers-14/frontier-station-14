namespace Content.Shared.Damage.Components;

public sealed partial class StaminaComponent : Component
{
    /// <summary>
    /// A dictionary of active stamina drains, with the key being the source of the drain,
    /// DrainRate how much it changes per tick, and ModifiesSpeed if it should slow down the user.
    /// 
    /// Used primarily for harpy flying.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, (float DrainRate, bool ModifiesSpeed)> ActiveDrains = new();
}
using Robust.Shared.GameStates;
namespace Content.Shared.Abilities;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RummagerComponent : Component
{
    // Frontier: cooldowns per-rummager
    /// <summary>
    /// Frontier: Last time this entity has rummaged, used to check if cooldown has expired
    /// </summary>
    [ViewVariables]
    public TimeSpan? LastRummaged;

    /// <summary>
    // Frontier: Minimum time between this entity's rummage attempts
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30.0f);
    // End Frontier
}

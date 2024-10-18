using Robust.Shared.GameStates;

namespace Content.Shared._NF.Emp.Components;

/// <summary>
///     Create circle pulse animation of emp around object.
///     Drawn on client after creation only once per component lifetime.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpBlastComponent : Component
{
    /// <summary>
    ///     Timestamp when component was assigned to this entity.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>
    ///     How long will animation play in seconds.
    ///     Can be overridden by <see cref="Robust.Shared.Spawners.TimedDespawnComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VisualDuration = 1f;

    /// <summary>
    ///     The range of animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VisualRange = 5f;
}

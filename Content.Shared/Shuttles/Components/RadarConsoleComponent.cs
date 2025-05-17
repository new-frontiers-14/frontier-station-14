using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause] // Frontier: add AutoGenerateComponentPause
[Access(typeof(SharedRadarConsoleSystem))]
public sealed partial class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(Owner, value, this);
    }

    [DataField, AutoNetworkedField]
    public float MaxRange = 256f;

    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public bool FollowEntity = false;

    // Frontier: ghost radar restrictions
    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public float? MaxIffRange = null;

    /// <summary>
    /// If true, the radar will not show the coordinates of objects on hover
    /// </summary>
    [DataField]
    public bool HideCoords = false;

    /// <summary>
    /// The last request time where blips were returned.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan LastRadarBlipRequest;
    // End Frontier
}

using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;
using System.Numerics; // Frontier

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    /// A settable target to display on IFF
    /// </summary>
    [DataField]
    public Vector2? Target;

    /// <summary>
    /// If not null, the target whose information will be displayed on the radar.
    /// </summary>
    [DataField]
    public EntityUid? TargetEntity;

    /// <summary>
    /// Whether or not to display the target IFF
    /// </summary>
    [DataField]
    public bool HideTarget = false;
    // End Frontier
}

using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared._NF.Roles.Components;

/// <summary>
/// Holds data pertaining to interview holograms
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InterviewHologramComponent : Component
{
    #region Hologram
    /// <summary>
    /// Name of the shader to use
    /// </summary>
    [DataField]
    public string ShaderName = string.Empty;

    /// <summary>
    /// The primary color
    /// </summary>
    [DataField]
    public Color Color1 = Color.White;

    /// <summary>
    /// The secondary color
    /// </summary>
    [DataField]
    public Color Color2 = Color.White;

    /// <summary>
    /// The shared color alpha
    /// </summary>
    [DataField]
    public float Alpha = 1f;

    /// <summary>
    /// The color brightness
    /// </summary>
    [DataField]
    public float Intensity = 1f;

    /// <summary>
    /// The scroll rate of the hologram shader
    /// </summary>
    [DataField]
    public float ScrollRate = 1f;

    /// <summary>
    /// The sprite offset
    /// </summary>
    [DataField]
    public Vector2 Offset = new Vector2();

    /// <summary>
    /// True if a character appearance has been applied to this entity.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool AppearanceApplied;
    #endregion Hologram

    #region Interview
    /// <summary>
    /// The job this user is applying for.
    /// </summary>
    [DataField]
    public EntityUid Station;

    /// <summary>
    /// The job this user is applying for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    /// <summary>
    /// True if the hologram user has approved this job.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplicantApproved;

    /// <summary>
    /// True if the captain has approved this job.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CaptainApproved;

    /// <summary>
    /// True if a character appearance has been applied to this entity.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool NotificationsSent;
    #endregion Interview

    #region Actions
    [DataField]
    public EntProtoId ToggleApprovalAction = "ActionInterviewToggleApproval";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleApprovalActionEntity;
    [DataField]
    public EntProtoId CancelApplicationAction = "ActionInterviewCancel";

    [DataField, AutoNetworkedField]
    public EntityUid? CancelApplicationActionEntity;
    #endregion Actions
}

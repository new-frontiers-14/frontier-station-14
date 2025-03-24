using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

/// <summary>
/// This is used for restricting anchoring pipes so that they do not overlap.
/// </summary>
[RegisterComponent, Access(typeof(DisposalPipeRestrictOverlapSystem))]
public sealed partial class DisposalPipeRestrictOverlapComponent : Component;

﻿using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components.ReplacementMarkers;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles the ability to replace entities marked with <see cref="ReinforcedWallReplacementMarkerComponent"/> in a variation pass
/// </summary>
public sealed class ReinforcedWallReplaceVariationPassSystem : BaseEntityReplaceVariationPassSystem<ReinforcedWallReplacementMarkerComponent, ReinforcedWallReplaceVariationPassComponent>
{
}

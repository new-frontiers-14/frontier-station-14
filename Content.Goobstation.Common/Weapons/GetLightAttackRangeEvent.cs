// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Goobstation.Common.Weapons;

[ByRefEvent]
public record struct GetLightAttackRangeEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);

[ByRefEvent]
public record struct LightAttackSpecialInteractionEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);

[ByRefEvent]
public record struct MeleeInRangeEvent(
    EntityUid User,
    EntityUid Target,
    float Range,
    EntityCoordinates? TargetCoordinates,
    Angle? TargetAngle,
    bool Handled = false,
    bool InRange = false);

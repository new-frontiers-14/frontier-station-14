// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.MobCaller;

namespace Content.Goobstation.Server.SpaceWhale;

/// <summary>
/// Marks an entity for a space whale target.
/// </summary>
[RegisterComponent]
public sealed partial class SpaceWhaleTargetComponent : Component
{
    [DataField]
    public EntityUid? MobCaller;

    [DataField]
    public EntProtoId<MobCallerComponent> MobCallerProto = "SpaceLeviathanMobCaller";
}

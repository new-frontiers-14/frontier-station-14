// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Goobstation.SpaceWhale;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TailedEntitySegmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates? Coords;

    [DataField, AutoNetworkedField]
    public Angle WorldRotation;

    [DataField, AutoNetworkedField]
    public int Order;

    [DataField, AutoNetworkedField]
    public int SegmentCount;

    [DataField, AutoNetworkedField]
    public EntityUid? Head;

    [DataField]
    public string? SegmentSpriteState;

    [DataField]
    public string? TailSpriteState;
}

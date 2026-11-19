// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Shared._Goobstation.SpaceWhale;

[ByRefEvent]
public record struct GetTailedEntitySegmentCountEvent(int Amount);

[ByRefEvent]
public readonly record struct UpdateTailedEntitySegmentCountEvent(int Amount);

public sealed partial class TailedEntityForceContractEvent : InstantActionEvent;

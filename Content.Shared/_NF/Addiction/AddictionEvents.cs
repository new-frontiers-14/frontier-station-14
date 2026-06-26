using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

[ByRefEvent]
public readonly record struct AddAddictionHighRatingEvent(ProtoId<AddictionPrototype> ProtoId, FixedPoint2 Amount);
[ByRefEvent]
public readonly record struct AddAddictionRatingEvent(ProtoId<AddictionPrototype> ProtoId, FixedPoint2 Amount);

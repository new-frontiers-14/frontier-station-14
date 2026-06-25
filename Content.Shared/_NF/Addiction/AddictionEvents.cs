using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

[ByRefEvent]
public readonly record struct AddAddictionRatingEvent(ProtoId<AddictionPrototype> ProtoId, int Amount);
[ByRefEvent]
public readonly record struct AddWithdrawalRatingEvent(ProtoId<AddictionPrototype> ProtoId, int Amount);

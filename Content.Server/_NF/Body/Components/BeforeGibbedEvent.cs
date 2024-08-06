namespace Content.Server.Body.Components;

/// <summary>
/// Raised before a body gets gibbed, before it is deleted.
/// </summary>
[ByRefEvent]
public readonly record struct BeforeGibbedEvent(EntityUid WillBeGibbed);

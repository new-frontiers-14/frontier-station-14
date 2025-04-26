namespace Content.Shared._NF.Interaction.Events;

/// <summary>
/// Raised on the used item when it was unsuccessfully used on another entity.
/// </summary>
[ByRefEvent]
public readonly record struct InteractionPopupOnUseFailureEvent(EntityUid Object, EntityUid User, EntityUid Target);

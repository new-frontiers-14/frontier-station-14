namespace Content.Shared._NF.Interaction.Events;

/// <summary>
/// Raised on the used item when it was successfully used on another entity.
/// </summary>
[ByRefEvent]
public readonly record struct InteractionPopupOnUseSuccessEvent(EntityUid Object, EntityUid User, EntityUid Target);

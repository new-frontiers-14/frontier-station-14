using Content.Shared.Actions;

namespace Content.Shared._NF.Mech.Components;

/// <summary>
/// An action raised on 
/// </summary>
public sealed partial class MechEquipmentEquippedAction : InstantActionEvent
{
    public EntityUid Mech { get; set; }
    public EntityUid? Pilot { get; set; }
}

namespace Content.Server._NF.Mech.Events;

[ByRefEvent]
public sealed class MechGrabberInteractEvent(EntityUid tool) : EntityEventArgs
{
    public EntityUid Tool = tool;
    public bool Handled = false;
}

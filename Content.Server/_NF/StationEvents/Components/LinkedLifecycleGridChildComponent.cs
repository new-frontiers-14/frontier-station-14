namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class LinkedLifecycleGridChildComponent : Component
{
    // The entity this grid's lifecycle is tied to.
    public EntityUid LinkedUid;
}

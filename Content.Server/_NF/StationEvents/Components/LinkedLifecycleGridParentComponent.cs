namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class LinkedLifecycleGridParentComponent : Component
{
    // The entity this grid's lifecycle is tied to.
    public HashSet<EntityUid> LinkedEntities = new();
}

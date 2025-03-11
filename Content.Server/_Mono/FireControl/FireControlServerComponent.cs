namespace Content.Server._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControlServerComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedGrid = null;

    [ViewVariables]
    public HashSet<EntityUid> Controlled = new();

    [ViewVariables]
    public HashSet<EntityUid> Consoles = new();

    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> Leases;
}

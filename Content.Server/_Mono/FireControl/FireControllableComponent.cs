namespace Content.Server._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControllableComponent : Component
{
    [ViewVariables]
    public EntityUid? ControllingServer = null;
}

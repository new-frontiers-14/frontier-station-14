namespace Content.Server.Shuttles;

/// <summary>
/// Lets you remotely control a shuttle.
/// </summary>
[RegisterComponent]
public sealed partial class NFDroneConsoleTargetComponent : Component
{
    [DataField(required: true)]
    public string Id = default!;
}

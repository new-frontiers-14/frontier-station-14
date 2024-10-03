namespace Content.Server.Shuttles.Components;

/// <summary>
/// Lets you remotely control a shuttle.
/// </summary>
[RegisterComponent]
public sealed partial class NFDroneConsoleComponent : Component
{
    [DataField(required: true)]
    public string Id = default!;

    /// <summary>
    /// <see cref="ShuttleConsoleComponent"/> that we're proxied into.
    /// </summary>
    [DataField]
    public EntityUid? Entity;
}

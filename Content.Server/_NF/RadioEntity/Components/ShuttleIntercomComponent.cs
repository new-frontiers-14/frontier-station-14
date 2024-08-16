using Robust.Shared.GameStates;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Append the intercom UI as a added verb
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleIntercomComponent : Component
{
    /// <summary>
    /// If true, appends the name of the station to any message sent over the console.
    /// </summary>
    [DataField]
    public bool AppendName = false;

    /// <summary>
    /// If non-null, replaces the name of the station with the given string when sending messages.
    /// </summary>
    [DataField]
    public string? OverrideName = null;
}

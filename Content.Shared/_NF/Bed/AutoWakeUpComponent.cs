using Robust.Shared.GameStates;

namespace Content.Shared._NF.Bed.Sleep;

/// <summary>
/// Frontier - Added to AI to allow auto waking up after 5 secs.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class AutoWakeUpComponent : Component
{
    // The length of time, in seconds, to sleep
    [DataField]
    public TimeSpan Length = TimeSpan.FromSeconds(5);

    [ViewVariables]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextWakeUp = TimeSpan.Zero;
}

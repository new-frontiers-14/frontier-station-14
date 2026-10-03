namespace Content.Server._NF.Shuttles.Components;

/// <summary>
///     This component is for a custom shipyard to track shuttles purchases.
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleCounterComponent : Component
{
    [DataField]
    public int Counter = 0;

    [DataField(required: true)]
    public int CountMax;
}

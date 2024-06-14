namespace Content.Shared.Stacks.Components;

[RegisterComponent]
public sealed partial class ThresholdStackLayerFunctionComponent : Component
{
    /// <summary>
    /// A list of thresholds to check against Amount.
    /// Each exceeded threshold will cause the next layer to be displayed.
    /// Should be sorted in ascending order.
    /// </summary>
    [DataField(required: true)]
    public List<int> Thresholds = new List<int>();
}

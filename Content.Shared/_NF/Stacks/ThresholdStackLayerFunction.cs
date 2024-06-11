

namespace Content.Shared.Stacks;

public sealed partial class ThresholdStackLayerFunction : StackLayerFunction
{
    /// <summary>
    /// A list of numbers to check against Amount.  Should be sorted in ascending order.
    /// </summary>
    [DataField(required: true)]
    public List<int> Thresholds;

    /// <summary>
    /// Sets Actual to the number of thresholds that Actual exceeds from the beginning of the list.
    /// Sets MaxCount to the total number of thresholds plus one (for values under thresholds).
    /// </summary>
    public override void Apply(ref StackLayerData data)
    {
        data.MaxCount = Math.Min(Thresholds.Count + 1, data.MaxCount);

        int newActual = 0;
        foreach (var threshold in Thresholds)
        {
            //Must ensure actual <= MaxCount.
            if (data.Actual >= threshold && newActual < data.MaxCount)
                newActual++;
            else
                break;
        }
        data.Actual = newActual;
    }
}

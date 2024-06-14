using Content.Shared.Stacks.Components;
using Content.Shared.Stacks;

namespace Content.Client.Stack
{
    /// <summary>
    /// Data used to determine which layers of a stack's sprite are visible.
    /// </summary>
    public struct StackLayerData
    {
        public int Actual;
        public int MaxCount;
        public bool Hidden;
    }

    public sealed partial class StackSystem : SharedStackSystem
    {
        // Modifies a given stack component to adjust the layers to display.
        private bool ApplyLayerFunction(EntityUid uid, StackComponent comp, ref StackLayerData data)
        {
            switch (comp.LayerFunction)
            {
                case StackLayerFunction.Threshold:
                    if (TryComp<StackLayerThresholdComponent>(uid, out var threshold))
                    {
                        ApplyThreshold(threshold, ref data);
                        return true;
                    }
                    break;
            }
            // No function applied.
            return false;
        }

        /// <summary>
        /// Sets Actual to the number of thresholds that Actual exceeds from the beginning of the list.
        /// Sets MaxCount to the total number of thresholds plus one (for values under thresholds).
        /// </summary>
        private static void ApplyThreshold(StackLayerThresholdComponent comp, ref StackLayerData data)
        {
            // We must stop before we run out of thresholds or layers, whichever's smaller. 
            data.MaxCount = Math.Min(comp.Thresholds.Count + 1, data.MaxCount);
            int newActual = 0;
            foreach (var threshold in comp.Thresholds)
            {
                //If our value exceeds threshold, the next layer should be displayed.
                //Note: we must ensure actual <= MaxCount.
                if (data.Actual >= threshold && newActual < data.MaxCount)
                    newActual++;
                else
                    break;
            }
            data.Actual = newActual;
        }
    }
}

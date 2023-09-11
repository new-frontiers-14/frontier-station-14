using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.AutoRegenReagent
{
    [RegisterComponent]
    public sealed partial class AutoRegenReagentComponent : Component
    {
        [DataField("solution", required: true)]
        public string? SolutionName = null; // we'll fail during tests otherwise

        [DataField("reagents", required: true)]
        public List<string> Reagents = default!;

        public string CurrentReagent = "";

        public int CurrentIndex = 0;

        public Solution? Solution = default!;

        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("unitsPerSecond")]
        public float unitsPerSecond = 0.2f;
    }
}

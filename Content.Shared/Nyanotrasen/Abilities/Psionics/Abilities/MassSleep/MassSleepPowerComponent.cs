using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MassSleepPowerComponent : Component
    {
        public WorldTargetAction? MassSleepPowerAction = null;

        public float Radius = 1.25f;
    }
}

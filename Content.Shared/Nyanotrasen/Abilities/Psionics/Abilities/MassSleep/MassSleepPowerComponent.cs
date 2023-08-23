using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class MassSleepPowerComponent : Component
    {
        public WorldTargetAction? MassSleepPowerAction = null;

        public float Radius = 1.25f;
    }
}
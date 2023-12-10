using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class NoosphericZapPowerComponent : Component
    {
        public EntityTargetAction? NoosphericZapPowerAction = null;
    }
}

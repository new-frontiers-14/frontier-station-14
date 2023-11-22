using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class DispelPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 10f;

        public EntityTargetAction? DispelPowerAction = null;
    }
}

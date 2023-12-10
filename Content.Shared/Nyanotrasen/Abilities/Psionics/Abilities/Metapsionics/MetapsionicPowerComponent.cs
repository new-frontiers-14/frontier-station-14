using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MetapsionicPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 5f;

        public InstantAction? MetapsionicPowerAction = null;
    }
}

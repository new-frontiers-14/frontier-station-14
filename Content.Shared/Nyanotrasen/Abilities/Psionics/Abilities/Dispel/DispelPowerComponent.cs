using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class DispelPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 10f;

        public EntityTargetAction? DispelPowerAction = null;
    }
}
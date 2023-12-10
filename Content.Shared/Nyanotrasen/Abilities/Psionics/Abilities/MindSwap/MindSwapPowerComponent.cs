using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MindSwapPowerComponent : Component
    {
        public EntityTargetAction? MindSwapPowerAction = null;
    }
}

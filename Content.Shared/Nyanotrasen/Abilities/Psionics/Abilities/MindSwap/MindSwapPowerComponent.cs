using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class MindSwapPowerComponent : Component
    {
        public EntityTargetAction? MindSwapPowerAction = null;
    }
}
namespace Content.Server.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class MindSwappedComponent : Component
    {
        [ViewVariables]
        public EntityUid OriginalEntity = default!;
    }
}

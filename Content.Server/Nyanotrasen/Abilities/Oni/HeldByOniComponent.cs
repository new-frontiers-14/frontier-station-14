namespace Content.Server.Abilities.Oni
{
    [RegisterComponent]
    public sealed class HeldByOniComponent : Component
    {
        public EntityUid Holder = default!;
    }
}

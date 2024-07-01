namespace Content.Server.Abilities.Oni
{
    [RegisterComponent]
    public sealed partial class HeldByOniComponent : Component
    {
        public EntityUid Holder = default!;

        // Frontier: wield accuracy fix
        public double minAngleAdded = 0.0;
        public double maxAngleAdded = 0.0;
        public double angleIncreaseAdded = 0.0;
        // End Frontier
    }
}

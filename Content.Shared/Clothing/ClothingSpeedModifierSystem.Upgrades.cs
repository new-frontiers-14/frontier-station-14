namespace Content.Shared.Clothing;

public sealed partial class ClothingSpeedModifierSystem
{
    public void ReduceClothingSlowdown(Entity<ClothingSpeedModifierComponent> ent, float amount)
    {
        // A suit that already speeds the wearer up must not be downgraded.
        if (ent.Comp.WalkModifier < 1)
            ent.Comp.WalkModifier = MathF.Min(1, ent.Comp.WalkModifier + amount);
        if (ent.Comp.SprintModifier < 1)
            ent.Comp.SprintModifier = MathF.Min(1, ent.Comp.SprintModifier + amount);
        Dirty(ent);
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
    }
}

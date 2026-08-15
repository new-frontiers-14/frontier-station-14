namespace Content.Shared._NF.Clothing.Components;

[RegisterComponent]
public sealed partial class WearerDamageModifierComponent : Component
{
    /// <summary>
    /// Every source of damage modification by clothing on this entity
    /// </summary>
    public HashSet<EntityUid> Sources = new();
}
namespace Content.Shared.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterFoodComponent : Component
{
    [DataField]
    public int Energy { get; set; } = 1;
}

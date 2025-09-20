namespace Content.Shared._NF.Weapons.Components;

[RegisterComponent]
public sealed partial class NFMarkerCounterComponent : Component
{
    [DataField] public int WhacksRemaining = 5;
}

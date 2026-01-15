namespace Content.Shared._Starlight.Antags.TerrorSpider;

[RegisterComponent]
public sealed partial class EggHolderComponent : Component
{
    [DataField]
    public int Counter = 0;
}

[RegisterComponent]
public sealed partial class HasEggHolderComponent : Component
{
}

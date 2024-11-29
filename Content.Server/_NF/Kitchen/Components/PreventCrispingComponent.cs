namespace Content.Server._NF.Kitchen.Components;

// Denotes that an item cannot become crispy in the deep fryer, but should burn normally.
[RegisterComponent]
public sealed partial class PreventCrispingComponent : Component
{
    // The number of cycles this has spent in the deep fryer.
    [ViewVariables]
    public int Cycles;
}

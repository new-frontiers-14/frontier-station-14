namespace Content.Shared._EE.FootPrint;

[RegisterComponent]
public sealed partial class PuddleFootPrintsComponent : Component
{
    [ViewVariables()]
    public float SizeRatio = 0.2f;

    [ViewVariables()]
    public float OffPercent = 80f;
}

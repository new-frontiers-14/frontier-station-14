namespace Content.Shared._NF.Foldable.Systems;

[RegisterComponent]
public sealed partial class FoldableFixtureComponent : Component
{
    [DataField(required: true)]
    public string? Fixture;
}

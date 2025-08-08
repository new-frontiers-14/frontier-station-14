using Content.Shared._NF.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Stacks.Components;

/// <summary>
/// Denotes an item that
/// </summary>
[RegisterComponent]
public sealed partial class RandomBlueprintComponent : Component
{
    [DataField(required: true)]
    public ProtoId<BlueprintPrototype> Blueprint;

    [DataField]
    public int MinRolls = 1;

    [DataField]
    public int MaxRolls = 1;
}

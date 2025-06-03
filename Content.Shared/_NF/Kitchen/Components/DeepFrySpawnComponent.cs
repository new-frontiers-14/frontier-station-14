using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Kitchen.Components;

[RegisterComponent] // Not networked, state keeping done for server, client access for guidebook.
public sealed partial class DeepFrySpawnComponent : Component
{
    // The number of cycles this item needs to fry before turning into something else.
    [DataField]
    public int Cycles = 1;

    // The prototype this is replaced by when fried long enough.
    [DataField(required: true)]
    public EntProtoId Output;
}

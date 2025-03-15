using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Changes a sprite depending on the number of entities in a container.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerCountVisualizerComponent : Component
{
    [DataField(required: true)]
    public int MaxFillLevels;
    [DataField(required: true)]
    public string ContainerName;
    [DataField(required: true)]
    public int MaxCount;

    [DataField(required: true)]
    public string FillBaseName = default!;
}

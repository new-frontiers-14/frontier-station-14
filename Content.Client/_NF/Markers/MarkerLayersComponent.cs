namespace Content.Client._NF.Markers;

[RegisterComponent]
public sealed partial class MarkerLayersComponent : Component
{
    [DataField, ViewVariables]
    public List<PrototypeLayerData> Layers = [];

    [ViewVariables]
    public readonly List<string> LayerKeys = [];
}

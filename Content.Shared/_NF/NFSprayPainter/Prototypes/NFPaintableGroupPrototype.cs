using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.NFSprayPainter.Prototypes;

[Prototype("NFPaintableGroup")]
public sealed partial class NFPaintableGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Category = string.Empty;

    [DataField]
    public NFPaintableVisuals Visuals = NFPaintableVisuals.BaseRSI;

    [DataField]
    public string? State = null;

    [DataField]
    public float Time = 2.0f;

    [DataField(required: true)]
    public Dictionary<string, NFPaintableData> AppearanceData = new();

    [DataField]
    public bool Duplicates = false;

    // The priority determines, which sprite is used when showing
    // the icon for a style in the SprayPainter UI. The highest priority
    // gets shown.
    [DataField]
    public int IconPriority = 0;
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct NFPaintableData
{
    public EntProtoId DisplayEntity;
    public string Data;
}

[Serializable, NetSerializable]
public enum NFPaintableVisuals
{
    BaseRSI,
    LockerRSI,
    CanisterState,
}

using Content.Shared.DoAfter;
using Content.Shared._NF.NFSprayPainter.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.NFSprayPainter;

[Serializable, NetSerializable]
public enum NFSprayPainterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NFSprayPainterSpritePickedMessage : BoundUserInterfaceMessage
{
    public readonly string Category;
    public readonly int Index;

    public NFSprayPainterSpritePickedMessage(string category, int index)
    {
        Category = category;
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class NFSprayPainterColorPickedMessage : BoundUserInterfaceMessage
{
    public readonly string? Key;

    public NFSprayPainterColorPickedMessage(string? key)
    {
        Key = key;
    }
}

[Serializable, NetSerializable]
public sealed class NFSprayPainterBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, int> SelectedStyles { get; }
    public string? SelectedColorKey { get; }
    public Dictionary<string, Color> Palette { get; }

    public NFSprayPainterBoundUserInterfaceState(Dictionary<string, int> selectedStyles, string? selectedColorKey, Dictionary<string, Color> palette)
    {
        SelectedStyles = selectedStyles;
        SelectedColorKey = selectedColorKey;
        Palette = palette;
    }
}

[Serializable, NetSerializable]
public sealed partial class NFSprayPainterDoAfterEvent : DoAfterEvent
{
    [DataField]
    public string Data;

    [DataField]
    public string Category;

    [DataField]
    public NFPaintableVisuals Visuals;

    public NFSprayPainterDoAfterEvent(string data, string category, NFPaintableVisuals visuals)
    {
        Data = data;
        Category = category;
        Visuals = visuals;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class NFSprayPainterPipeDoAfterEvent : DoAfterEvent
{
    [DataField]
    public Color Color;

    public NFSprayPainterPipeDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}

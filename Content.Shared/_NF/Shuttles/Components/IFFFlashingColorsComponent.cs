using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shuttles.Components;

/// <summary>
/// Component added to shuttles that are able to set a Flashing IFF.
/// Usually added to medical and security shuttles.
///
/// IFF color will shift between each colors within a set timeframe.
/// </summary>
[RegisterComponent]
public sealed partial class IFFFlashingColorsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public int CurrentIFFColorIndex = 0;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ChangeIndexAt = TimeSpan.MaxValue;
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public Color? OriginalColor;
    [ViewVariables(VVAccess.ReadOnly)]
    public Color CurColor = Color.White;
    [ViewVariables(VVAccess.ReadOnly)]
    public Color NextColor = Color.White;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CurDuration = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public IFFFlashingColorsSequencePrototype? CurrentIFFFlashingSequence;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<IFFFlashingColorsSequencePrototype>> AllowedIFFFlashingSequences = new();
}

[Prototype("IFFFlashingColorsSequence")]
public sealed partial class IFFFlashingColorsSequencePrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public List<IFFFlashingColorState> FlashingColors = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class IFFFlashingColorState
{
    [DataField]
    public Color Color;

    [DataField]
    public TimeSpan Duration;


    /// <summary>
    /// If true, the IFF color will gradually blend from the current color into the next color instead of switching when duration is up.
    /// Otherwise, the IFF color will switch  once the duration is up.
    /// </summary>
    [DataField]
    public bool IsBlended = true;
}

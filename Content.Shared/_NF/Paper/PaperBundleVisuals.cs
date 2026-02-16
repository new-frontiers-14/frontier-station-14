using Robust.Shared.Serialization;

namespace Content.Shared._NF.Paper;

/// <summary>
/// Appearance data keys for paper bundle visual states.
/// </summary>
[Serializable, NetSerializable]
public enum PaperBundleVisuals : byte
{
    /// <summary>
    /// Whether any page in the bundle has written content or stamps.
    /// </summary>
    HasContent
}

/// <summary>
/// Sprite layer keys for the paper bundle entity.
/// </summary>
[Serializable, NetSerializable]
public enum PaperBundleVisualLayers : byte
{
    /// <summary>
    /// The base sprite layer showing the paper stack.
    /// </summary>
    Base
}

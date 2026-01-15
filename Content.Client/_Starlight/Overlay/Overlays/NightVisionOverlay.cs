using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay;

public sealed class NightVisionOverlay : BaseVisionOverlay
{
    public NightVisionOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int?) OverlayZIndexes.NightVision;
    }
}

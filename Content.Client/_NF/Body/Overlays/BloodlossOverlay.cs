using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Body.Overlays;

/// <summary>
/// Fullscreen desaturation overlay that fades the world to grayscale (preserving the species'
/// blood color) as the player loses blood. Intensity is driven by <see cref="Systems.BloodlossOverlaySystem"/>.
/// </summary>
public sealed class BloodlossOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Bloodloss";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _bloodlossShader;

    /// <summary>
    /// Current desaturation intensity (0.0 = no effect, 1.0 = full grayscale).
    /// Set by <see cref="Systems.BloodlossOverlaySystem"/> each frame.
    /// </summary>
    public float CurrentIntensity;

    /// <summary>
    /// Species blood color passed to the shader for hue preservation.
    /// Set by <see cref="Systems.BloodlossOverlaySystem"/> each frame.
    /// </summary>
    public Color BloodColor = Color.FromHex("#800000");

    public BloodlossOverlay()
    {
        IoCManager.InjectDependencies(this);
        _bloodlossShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = 5;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (CurrentIntensity <= 0f)
            return false;

        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _bloodlossShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _bloodlossShader.SetParameter("bloodlossIntensity", CurrentIntensity);
        _bloodlossShader.SetParameter("bloodColor", new Vector3(BloodColor.R, BloodColor.G, BloodColor.B));
        handle.UseShader(_bloodlossShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}

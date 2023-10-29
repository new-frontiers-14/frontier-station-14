using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Park.Overlays.Shaders;

/// <summary>
///     A simple overlay that applies a colored tint to the screen.
/// </summary>
public sealed class ColorTintOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    // private readonly ShaderInstance _shader;

    /// <summary>
    ///     The color to tint the screen to as RGB on a scale of 0-1.
    /// </summary>
    public Vector3? TintColor = null;
    /// <summary>
    ///     The percent to tint the screen by on a scale of 0-1.
    /// </summary>
    public float? TintAmount = null;
    /// <summary>
    ///     Component required to be on the entity to tint the screen.
    /// </summary>
    public Component? Comp = null;

    public ColorTintOverlay()
    {
        IoCManager.InjectDependencies(this);

        // _shader = _prototype.Index<ShaderPrototype>("ColorTint").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // if (ScreenTexture == null ||
        //     _player.LocalPlayer?.ControlledEntity is not { Valid: true } player ||
        //     Comp != null && !_entity.HasComponent(player, Comp.GetType()))
        //     return;

        // _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        // if (TintColor != null)
        //     _shader.SetParameter("tint_color", (Vector3) TintColor);
        // if (TintAmount != null)
        //     _shader.SetParameter("tint_amount", (float) TintAmount);

        // var worldHandle = args.WorldHandle;
        // var viewport = args.WorldBounds;
        // worldHandle.SetTransform(Matrix3.Identity);
        // worldHandle.UseShader(_shader);
        // worldHandle.DrawRect(viewport, Color.White);
        // worldHandle.UseShader(null);
    }
}

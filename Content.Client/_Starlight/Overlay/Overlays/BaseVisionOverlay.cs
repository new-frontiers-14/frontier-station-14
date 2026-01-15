using Content.Shared.Eye.Blinding.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay;

/*
Time to mini rant here. this NEEDs to be a abstract because if its not, then
the overlay system will think its trying to apply multiple of the same overlay
Hence, itll remove all of them until theres only one, even if all the instances
youve added are different based on their fields. So to get around this,
we define all the actual BEHAVIOUR here, but then just make it appear as a new
type by inheriting from this and implementing nothing. Thanks Robust Toolbox team.
*/
public abstract class BaseVisionOverlay : global::Robust.Client.Graphics.Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private protected readonly ShaderInstance _shader;

    protected BaseVisionOverlay(ShaderPrototype shader)
    {
        IoCManager.InjectDependencies(this);
        _shader = shader.InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;
        if (playerEntity == null)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}

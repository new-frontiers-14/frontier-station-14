using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._Park.Overlays.Shaders;

public sealed class IgnoreHumanoidWithComponentOverlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public List<Component> IgnoredComponents = new();
    public List<Component> AllowAnywayComponents = new();
    private readonly List<EntityUid> _nonVisibleList = new();

    public IgnoreHumanoidWithComponentOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var spriteQuery = _entityManager.GetEntityQuery<SpriteComponent>();

        foreach (var humanoid in _entityManager.EntityQuery<HumanoidAppearanceComponent>(true))
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == humanoid.Owner)
                continue;

            var cont = true;
            foreach (var comp in IgnoredComponents)
            {
                if (!_entityManager.HasComponent(humanoid.Owner, comp.GetType()))
                    continue;

                cont = false;
                break;
            }
            foreach (var comp in AllowAnywayComponents)
            {
                if (!_entityManager.HasComponent(humanoid.Owner, comp.GetType()))
                    continue;

                cont = true;
                break;
            }
            if (cont)
            {
                Reset(humanoid.Owner);
                continue;
            }


            if (!spriteQuery.TryGetComponent(humanoid.Owner, out var sprite))
                continue;

            if (!sprite.Visible || _nonVisibleList.Contains(humanoid.Owner))
                continue;

            sprite.Visible = false;
            _nonVisibleList.Add(humanoid.Owner);
        }

        foreach (var humanoid in _nonVisibleList.ToArray())
        {
            if (!_entityManager.Deleted(humanoid))
                continue;

            _nonVisibleList.Remove(humanoid);
        }
    }


    public void Reset()
    {
        foreach (var humanoid in _nonVisibleList.ToArray())
        {
            _nonVisibleList.Remove(humanoid);

            if (_entityManager.TryGetComponent<SpriteComponent>(humanoid, out var sprite))
                sprite.Visible = true;
        }
    }

    public void Reset(EntityUid entity)
    {
        if (!_nonVisibleList.Contains(entity))
            return;

        _nonVisibleList.Remove(entity);

        if (_entityManager.TryGetComponent<SpriteComponent>(entity, out var sprite))
            sprite.Visible = true;
    }
}

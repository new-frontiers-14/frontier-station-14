using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Network;
using Content.Shared.Tag;
using Content.Shared._Park.Traits;

namespace Content.Client._Park.Overlays;
public sealed class NearsightedSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private NearsightedOverlay _overlay = default!;
    private NearsightedComponent nearsight = new();

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new Overlays.NearsightedOverlay();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var nearsight in EntityQuery<NearsightedComponent>())
        {
            var Tags = EnsureComp<TagComponent>(nearsight.Owner);
            if (Tags.Tags.Contains("GlassesNearsight")) UpdateShader(nearsight, true);
            else UpdateShader(nearsight, false);
        }
    }


    private void UpdateShader(NearsightedComponent component, bool booLean)
    {
        while (_overlayMan.HasOverlay<NearsightedOverlay>()) _overlayMan.RemoveOverlay(_overlay);
        component.Glasses = booLean;
        _overlayMan.AddOverlay(_overlay);
    }
}

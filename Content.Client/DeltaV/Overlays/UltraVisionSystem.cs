using Content.Shared.DeltaV.Abilities;
using Robust.Client.Graphics;

namespace Content.Client.DeltaV.Overlays;

public sealed partial class UltraVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private UltraVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UltraVisionComponent, ComponentInit>(OnUltraVisionInit);
        SubscribeLocalEvent<UltraVisionComponent, ComponentShutdown>(OnUltraVisionShutdown);
        
        _overlay = new();
    }

    private void OnUltraVisionInit(EntityUid uid, UltraVisionComponent component, ComponentInit args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnUltraVisionShutdown(EntityUid uid, UltraVisionComponent component, ComponentShutdown args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}

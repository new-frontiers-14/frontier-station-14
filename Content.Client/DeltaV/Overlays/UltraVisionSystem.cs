using Robust.Client.Graphics;
using Content.Shared.Abilities; # DeltaV
using Content.Shared.DeltaV.CCVars; # DeltaV
using Robust.Shared.Configuration; # DeltaV

namespace Content.Client.DeltaV.Overlays;

public sealed partial class UltraVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; # DeltaV

    private UltraVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UltraVisionComponent, ComponentInit>(OnUltraVisionInit);
        SubscribeLocalEvent<UltraVisionComponent, ComponentShutdown>(OnUltraVisionShutdown);

        Subs.CVar(_cfg, DCCVars.NoVisionFilters, OnNoVisionFiltersChanged); # DeltaV

        _overlay = new();
    }

    private void OnUltraVisionInit(EntityUid uid, UltraVisionComponent component, ComponentInit args)
    {
        if (!_cfg.GetCVar(DCCVars.NoVisionFilters)) # DeltaV
            _overlayMan.AddOverlay(_overlay); # DeltaV
    }

    private void OnUltraVisionShutdown(EntityUid uid, UltraVisionComponent component, ComponentShutdown args)
    {
        _overlayMan.RemoveOverlay(_overlay); # DeltaV
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
            _overlayMan.RemoveOverlay(_overlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }
}

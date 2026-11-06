using Content.Client._Emberfall.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Emberfall.Weapons.Ranged.Overlays;

public sealed class TracerOverlay : Overlay
{
    private readonly TracerSystem _tracer;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TracerOverlay(TracerSystem tracer)
    {
        _tracer = tracer;
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _tracer.Draw(args.WorldHandle, args.MapId);
    }
}

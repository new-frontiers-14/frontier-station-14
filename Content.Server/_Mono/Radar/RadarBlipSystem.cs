using System.Numerics;
using Content.Shared._Mono.Radar;
using Content.Shared.Shuttles.Components;

namespace Content.Server._Mono.Radar;

public sealed partial class RadarBlipSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestBlipsEvent>(OnBlipsRequested);
    }

    private void OnBlipsRequested(RequestBlipsEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Radar, out var radarUid))
            return;

        if (!TryComp<RadarConsoleComponent>(radarUid, out var radar))
            return;

        var blips = AssembleBlipsReport((EntityUid)radarUid, radar);

        var giveEv = new GiveBlipsEvent(blips);
        RaiseNetworkEvent(giveEv, args.SenderSession);
    }

    private List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> AssembleBlipsReport(EntityUid uid, RadarConsoleComponent? component = null)
    {
        var blips = new List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)>();

        if (Resolve(uid, ref component))
        {
            var radarXform = Transform(uid);
            var radarPosition = _xform.GetWorldPosition(uid);
            var radarGrid = _xform.GetGrid(uid);
            
            var blipQuery = EntityQueryEnumerator<RadarBlipComponent, TransformComponent>();

            while (blipQuery.MoveNext(out var blipUid, out var blip, out var blipXform))
            {
                if (!blip.Enabled)
                    continue;

                var blipPosition = _xform.GetWorldPosition(blipUid);
                var distance = (blipPosition - radarPosition).Length();
                if (distance > component.MaxRange)
                    continue;

                var blipGrid = _xform.GetGrid(blipUid);

                if (blip.RequireNoGrid)
                {
                    if (blipGrid != null)
                        continue;
                    
                    // For free-floating blips without a grid, use world position with null grid
                    blips.Add((null, blipPosition, blip.Scale, blip.RadarColor, blip.Shape));
                }
                else
                {
                    // If we're requiring grid, make sure they're on the same grid
                    if (blipGrid != radarGrid)
                        continue;
                    
                    // For grid-aligned blips, store grid NetEntity and grid-local position
                    if (blipGrid != null)
                    {
                        // Local position relative to grid 
                        var gridMatrix = _xform.GetWorldMatrix(blipGrid.Value);
                        Matrix3x2.Invert(gridMatrix, out var invGridMatrix);
                        var localPos = Vector2.Transform(blipPosition, invGridMatrix);
                        
                        // Add grid-relative blip with grid entity ID
                        blips.Add((GetNetEntity(blipGrid.Value), localPos, blip.Scale, blip.RadarColor, blip.Shape));
                    }
                    else
                    {
                        // Fallback to world position with null grid
                        blips.Add((null, blipPosition, blip.Scale, blip.RadarColor, blip.Shape));
                    }
                }
            }
        }

        return blips;
    }
}

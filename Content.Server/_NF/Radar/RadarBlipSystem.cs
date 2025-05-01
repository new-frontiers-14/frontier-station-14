using System.Numerics;
using Content.Shared._NF.Radar;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server._NF.Radar;

public sealed partial class RadarBlipSystem : EntitySystem
{
    private const double BlipStaleSeconds = 1.0;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestBlipsEvent>(OnBlipsRequested);
    }

    /// <summary>
    /// Handles a network request for radar blips and sends the blip data to the requesting client.
    /// </summary>
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

    /// <summary>
    /// Assembles a list of radar blips visible to the given radar console.
    /// </summary>
    private List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> AssembleBlipsReport(EntityUid uid, RadarConsoleComponent? component = null)
    {
        var blips = new List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)>();

        if (!Resolve(uid, ref component))
            return blips;

        var radarXform = Transform(uid);
        var radarPosition = _xform.GetWorldPosition(uid);
        var radarGrid = _xform.GetGrid(uid);
        var radarMapId = radarXform.MapID;

        var blipQuery = EntityQueryEnumerator<RadarBlipComponent, TransformComponent>();

        while (blipQuery.MoveNext(out var blipUid, out var blip, out var blipXform))
        {
            if (!blip.Enabled)
            {
                Log.Debug($"Blip {blipUid} skipped: not enabled.");
                continue;
            }

            if (ShouldSkipProjectileBlip(blipUid, blipXform, radarMapId))
                continue;

            var blipPosition = _xform.GetWorldPosition(blipUid);
            var distance = (blipPosition - radarPosition).Length();
            if (distance > component.MaxRange)
            {
                Log.Debug($"Blip {blipUid} skipped: out of range.");
                continue;
            }

            var blipGrid = _xform.GetGrid(blipUid);

            if (blip.RequireNoGrid)
            {
                if (blipGrid != null)
                {
                    Log.Debug($"Blip {blipUid} skipped: has grid but requires none.");
                    continue;
                }
                blips.Add((null, blipPosition, blip.Scale, blip.RadarColor, blip.Shape));
            }
            else if (blip.VisibleFromOtherGrids)
            {
                AddGridOrWorldBlip(blips, blipGrid, blipPosition, blip);
            }
            else
            {
                if (blipGrid != radarGrid)
                {
                    Log.Debug($"Blip {blipUid} skipped: not on same grid as radar.");
                    continue;
                }
                AddGridOrWorldBlip(blips, blipGrid, blipPosition, blip);
            }
        }
        return blips;
    }

    /// <summary>
    /// Determines if a projectile blip should be skipped based on map context.
    /// </summary>
    private bool ShouldSkipProjectileBlip(EntityUid blipUid, TransformComponent blipXform, MapId radarMapId)
    {
        if (TryComp<ProjectileComponent>(blipUid, out var projectile))
        {
            if (blipXform.MapID != radarMapId)
            {
                Log.Debug($"Projectile blip {blipUid} skipped: different map.");
                return true;
            }
            if (projectile.Shooter != null &&
                TryComp<TransformComponent>(projectile.Shooter, out var shooterXform) &&
                shooterXform.MapID != blipXform.MapID)
            {
                Log.Debug($"Projectile blip {blipUid} skipped: shooter on different map.");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Adds a blip to the list, converting to grid-local coordinates if needed.
    /// </summary>
    private void AddGridOrWorldBlip(List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> blips, EntityUid? blipGrid, Vector2 blipPosition, RadarBlipComponent blip)
    {
        if (blipGrid != null)
        {
            var gridMatrix = _xform.GetWorldMatrix(blipGrid.Value);
            Matrix3x2.Invert(gridMatrix, out var invGridMatrix);
            var localPos = Vector2.Transform(blipPosition, invGridMatrix);
            blips.Add((GetNetEntity(blipGrid.Value), localPos, blip.Scale, blip.RadarColor, blip.Shape));
        }
        else
        {
            blips.Add((null, blipPosition, blip.Scale, blip.RadarColor, blip.Shape));
        }
    }
}

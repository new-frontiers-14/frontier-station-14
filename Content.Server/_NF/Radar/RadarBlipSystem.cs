using System.Numerics;
using Content.Shared._Goobstation.Vehicles;
using Content.Shared._NF.Radar;
using Content.Shared.GameTicking;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._NF.Radar;

/// <summary>
/// A system that handles and rate-limits client-made requests for radar blips.
/// </summary>
/// <remarks>
/// Ported from Monolith's RadarBlipsSystem.
/// </remarks>
public sealed partial class RadarBlipSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private Dictionary<NetUserId, TimeSpan> _nextBlipRequestPerUser = new();

    // The minimum amount of time between handled blip requests.
    private static readonly TimeSpan MinRequestPeriod = TimeSpan.FromSeconds(1);
    // Maximum distance for blips to be considered visible
    private const float MaxBlipRenderDistance = 300f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestBlipsEvent>(OnBlipsRequested);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<ActiveJetpackComponent, ComponentStartup>(OnJetpackActivated);
        SubscribeLocalEvent<ActiveJetpackComponent, ComponentShutdown>(OnJetpackDeactivated);
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

        if (_nextBlipRequestPerUser.TryGetValue(args.SenderSession.UserId, out var requestTime) && _timing.RealTime < requestTime)
            return;

        _nextBlipRequestPerUser[args.SenderSession.UserId] = _timing.RealTime + MinRequestPeriod;

        var blips = AssembleBlipsReport((radarUid.Value, radar));

        var giveEv = new GiveBlipsEvent(blips);
        RaiseNetworkEvent(giveEv, args.SenderSession);
    }

    /// <summary>
    /// Clears blip request data between rounds.
    /// </summary>
    public void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _nextBlipRequestPerUser.Clear();
    }

    /// <summary>
    /// Assembles a list of radar blips visible to the given radar console.
    /// </summary>
    private List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> AssembleBlipsReport(Entity<RadarConsoleComponent> ent)
    {
        var blips = new List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)>();

        if (!TryComp(ent, out TransformComponent? radarXform))
            return blips;
        var radarPosition = _xform.GetWorldPosition(ent);
        var radarGrid = radarXform.GridUid;
        var radarMapId = radarXform.MapID;
        var radarRange = MathF.Min(ent.Comp.MaxRange, MaxBlipRenderDistance);

        // Non-positive range, nothing to return.
        if (radarRange <= 0)
            return blips;

        var blipQuery = EntityQueryEnumerator<RadarBlipComponent, TransformComponent>();

        while (blipQuery.MoveNext(out var blipUid, out var blip, out var blipXform))
        {
            if (!blip.Enabled)
            {
                Log.Debug($"Blip {blipUid} skipped: not enabled.");
                continue;
            }

            if (blipXform.MapID != radarMapId)
            {
                Log.Debug($"Blip {blipUid} skipped: different map.");
                continue;
            }

            // Run cheaper grid checks before distance checks
            var blipGrid = blipXform.GridUid;
            if (blip.RequireNoGrid && blipGrid != null)
            {
                Log.Debug($"Blip {blipUid} skipped: has grid but requires none.");
                continue;
            }

            if (!blip.VisibleFromOtherGrids && blipGrid != radarGrid)
            {
                Log.Debug($"Blip {blipUid} skipped: not on same grid as radar.");
                continue;
            }

            var blipPosition = _xform.GetWorldPosition(blipUid);
            var distance = (blipPosition - radarPosition).Length();
            if (distance > radarRange)
            {
                Log.Debug($"Blip {blipUid} skipped: out of range.");
                continue;
            }

            // Convert blip position to grid coords if needed.
            NetEntity? blipNetGrid = null;
            if (blipGrid != null)
            {
                blipNetGrid = GetNetEntity(blipGrid.Value);
                blipPosition = Vector2.Transform(blipPosition, _xform.GetInvWorldMatrix(blipGrid.Value));
            }
            blips.Add((blipNetGrid, blipPosition, blip.Scale, blip.RadarColor, blip.Shape));
        }
        return blips;
    }

    /// <summary>
    /// Configures the radar blip for a jetpack or vehicle entity.
    /// </summary>
    private void SetupRadarBlip(EntityUid uid, Color color, float scale, bool visibleFromOtherGrids = true, bool requireNoGrid = false)
    {
        var blip = EnsureComp<RadarBlipComponent>(uid);
        blip.RadarColor = color;
        blip.Scale = scale;
        blip.VisibleFromOtherGrids = visibleFromOtherGrids;
        blip.RequireNoGrid = requireNoGrid;
    }

    /// <summary>
    /// Adds radar blip to jetpacks when they are activated.
    /// </summary>
    private void OnJetpackActivated(EntityUid uid, ActiveJetpackComponent component, ComponentStartup args)
    {
        SetupRadarBlip(uid, Color.Cyan, 1f, true, true);
    }

    /// <summary>
    /// Removes radar blip from jetpacks when they are deactivated.
    /// </summary>
    private void OnJetpackDeactivated(EntityUid uid, ActiveJetpackComponent component, ComponentShutdown args)
    {
        RemComp<RadarBlipComponent>(uid);
    }

    /// <summary>
    /// Configures the radar blip for a vehicle entity.
    /// </summary>
    public void SetupVehicleRadarBlip(Entity<VehicleComponent> uid)
    {
        SetupRadarBlip(uid, Color.Cyan, 1f, true, true);
    }
}

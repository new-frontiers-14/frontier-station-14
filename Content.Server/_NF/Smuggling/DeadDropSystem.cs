using System.Text;
using Content.Server._NF.Smuggling.Components;
using Content.Server.Administration.Logs;
using Content.Server.Paper;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shipyard.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Shuttles.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._NF.Smuggling;

public sealed class DeadDropSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeadDropComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DeadDropComponent, GetVerbsEvent<InteractionVerb>>(AddSearchVerb);
    }

    private void OnStartup(EntityUid paintingUid, DeadDropComponent component, ComponentStartup _)
    {
        component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
    }

    private void AddSearchVerb(EntityUid uid, DeadDropComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null || _timing.CurTime < component.NextDrop)
            return;
        InteractionVerb searchVerb = new()
        {
            IconEntity = GetNetEntity(uid),
            Act = () => SendDeadDrop(uid, component, args.User, args.Hands),
            Text = Loc.GetString("deaddrop-search-text"),
            Priority = 3
        };

        args.Verbs.Add(searchVerb);
    }

    private void SendDeadDrop(EntityUid uid, DeadDropComponent component, EntityUid user, HandsComponent hands)
    {
        if (_timing.CurTime < component.NextDrop)
            return;

        if (_shipyard.ShipyardMap is not MapId shipyardMap)
            return;

        var options = new MapLoadOptions
        {
            LoadMap = false,
        };

        if (!_map.TryLoad(shipyardMap, component.DropGrid, out var gridUids, options))
            return;

        _shuttle.SetIFFColor(gridUids[0], component.Color);
        _shuttle.AddIFFFlag(gridUids[0], IFFFlags.HideLabel);

        var dropLocation = _random.NextVector2(component.MinimumDistance, component.MaximumDistance);
        var mapId = Transform(user).MapID;
        var coords = new MapCoordinates(dropLocation, mapId);
        var location = Spawn(null, coords);

        if (TryComp<ShuttleComponent>(gridUids[0], out var shuttle))
        {
            _shuttle.FTLTravel(gridUids[0], shuttle, location, 5.5f, 35f);
        }

        var channel = _prototypeManager.Index<RadioChannelPrototype>("Security");
        var sender = Transform(user).GridUid ?? uid;
        _radio.SendRadioMessage(sender, Loc.GetString("deaddrop-security-report"), channel, uid);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user)} activated a dead drop from {ToPrettyString(uid)} at {Transform(uid).Coordinates.ToString()}");

        var dropHint = new StringBuilder();

        dropHint.AppendLine(Loc.GetString("deaddrop-hint-pretext"));
        dropHint.AppendLine();
        dropHint.AppendLine(dropLocation.ToString());
        dropHint.AppendLine();
        dropHint.AppendLine(Loc.GetString("deaddrop-hint-posttext"));

        var paper = EntityManager.SpawnEntity(component.HintPaper, Transform(uid).Coordinates);
        _paper.SetContent(paper, dropHint.ToString());
        _meta.SetEntityName(paper, Loc.GetString("deaddrop-hint-name"));
        _meta.SetEntityDescription(paper, Loc.GetString("deaddrop-hint-desc"));
        _hands.PickupOrDrop(user, paper, handsComp: hands);

        component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
    }
}

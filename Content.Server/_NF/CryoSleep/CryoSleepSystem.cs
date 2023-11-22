using System.Numerics;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.ActionBlocker;
using Content.Shared.Climbing.Systems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Server.CryoSleep;

public sealed class CryoSleepSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly MindSystem _mind = default!;

    private EntityUid? _storageMap;
    // TODO: add a proper doafter system once that all gets sorted out
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoSleepComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<CryoSleepComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoSleepComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<CryoSleepComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CryoSleepComponent, DestructionEventArgs>((e,c,_) => EjectBody(e, c));
    }

    private EntityUid GetStorageMap()
    {
        if (Deleted(_storageMap))
        {
            var map = _mapManager.CreateMap();
            _storageMap = _mapManager.GetMapEntityId(map);
            _mapManager.SetMapPaused(map, true);
        }

        return _storageMap.Value;
    }

    private void OnInit(EntityUid uid, CryoSleepComponent component, ComponentStartup args)
    {
        component.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "body_container");
    }

    private void AddAlternativeVerbs(EntityUid uid, CryoSleepComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (IsOccupied(component))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant")
            };
            args.Verbs.Add(verb);
        }

        // Self-insert verb
        if (!IsOccupied(component) &&
            _actionBlocker.CanMove(args.User))
        {
            AlternativeVerb verb = new()
            {
                Act = () => InsertBody(args.User, component, false),
                Category = VerbCategory.Insert,
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnSuicide(EntityUid uid, CryoSleepComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (args.Victim != component.BodyContainer.ContainedEntity)
            return;

        QueueDel(args.Victim);
        _audio.PlayPvs(component.LeaveSound, uid);
        args.SetHandled(SuicideKind.Special);
    }

    private void OnExamine(EntityUid uid, CryoSleepComponent component, ExaminedEvent args)
    {
        var message = component.BodyContainer.ContainedEntity == null
            ? "cryopod-examine-empty"
            : "cryopod-examine-occupied";

        args.PushMarkup(Loc.GetString(message));
    }

    public bool InsertBody(EntityUid? toInsert, CryoSleepComponent component, bool force)
    {
        if (toInsert == null)
            return false;

        if (IsOccupied(component) && !force)
            return false;

        if (_mind.TryGetMind(toInsert.Value, out var mind, out var mindComp))
        {
            var session = mindComp.Session;
            if (session is not null && session.Status == SessionStatus.Disconnected)
            {
                CryoStoreBody(toInsert.Value);
                return true;
            }
        }

        var success = component.BodyContainer.Insert(toInsert.Value, EntityManager);

        if (success && mindComp?.Session != null)
        {
            _euiManager.OpenEui(new CryoSleepEui(mind, this), mindComp.Session);
        }

        return success;
    }

    public void CryoStoreBody(EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.CurrentEntity is not { Valid : true } body)
        {
            QueueDel(mindId);
            return;
        }

        _gameTicker.OnGhostAttempt(mindId, false, true, mind: mind);
        var storage = GetStorageMap();
        var xform = Transform(body);
        xform.Coordinates = new EntityCoordinates(storage, Vector2.Zero);
    }

    public bool EjectBody(EntityUid pod, CryoSleepComponent component)
    {
        if (!IsOccupied(component))
            return false;

        var toEject = component.BodyContainer.ContainedEntity;
        if (toEject == null)
            return false;

        component.BodyContainer.Remove(toEject.Value);
        _climb.ForciblySetClimbing(toEject.Value, pod);

        return true;
    }

    private bool IsOccupied(CryoSleepComponent component)
    {
        return component.BodyContainer.ContainedEntity != null;
    }
}


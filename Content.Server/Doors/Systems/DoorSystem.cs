using Content.Server.Access;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Shared.Database;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Shared.Tools;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly DoorBoltSystem _bolts = default!;
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ConstructionSystem) });

        // Mob prying doors
        SubscribeLocalEvent<DoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorAltVerb);

        SubscribeLocalEvent<DoorComponent, DoorPryDoAfterEvent>(OnPryFinished);
        SubscribeLocalEvent<DoorComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<DoorComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected override void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        // TODO once access permissions are shared, move this back to shared.
        if (args.Handled || !door.ClickOpen)
            return;

        TryToggleDoor(uid, door, args.User);
        args.Handled = true;
    }

    protected override void SetCollidable(
        EntityUid uid,
        bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.ChangeAirtight && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked(uid, airtight, collidable);

        // Pathfinding / AI stuff.
        RaiseLocalEvent(new AccessReaderChangeEvent(uid, collidable));

        base.SetCollidable(uid, collidable, door, physics, occluder);
    }

    // TODO AUDIO PREDICT Figure out a better way to handle sound and prediction. For now, this works well enough?
    //
    // Currently a client will predict when a door is going to close automatically. So any client in PVS range can just
    // play their audio locally. Playing it server-side causes an odd delay, while in shared it causes double-audio.
    //
    // But if we just do that, then if a door is closed prematurely as the result of an interaction (i.e., using "E" on
    // an open door), then the audio would only be played for the client performing the interaction.
    //
    // So we do this:
    // - Play audio client-side IF the closing is being predicted (auto-close or predicted interaction)
    // - Server assumes automated closing is predicted by clients and does not play audio unless otherwise specified.
    // - Major exception is player interactions, which other players cannot predict
    // - In that case, send audio to all players, except possibly the interacting player if it was a predicted
    //   interaction.

    /// <summary>
    /// Selectively send sound to clients, taking care to not send the double-audio.
    /// </summary>
    /// <param name="uid">The audio source</param>
    /// <param name="soundSpecifier">The sound</param>
    /// <param name="audioParams">The audio parameters.</param>
    /// <param name="predictingPlayer">The user (if any) that instigated an interaction</param>
    /// <param name="predicted">Whether this interaction would have been predicted. If the predicting player is null,
    /// this assumes it would have been predicted by all players in PVS range.</param>
    protected override void PlaySound(EntityUid uid, SoundSpecifier soundSpecifier, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted)
    {
        // If this sound would have been predicted by all clients, do not play any audio.
        if (predicted && predictingPlayer == null)
            return;

        if (predicted)
            Audio.PlayPredicted(soundSpecifier, uid, predictingPlayer, audioParams);
        else
            Audio.PlayPvs(soundSpecifier, uid, audioParams);
    }

#region DoAfters
    /// <summary>
    ///     Weld or pry open a door.
    /// </summary>
    private void OnInteractUsing(EntityUid uid, DoorComponent door, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool))
            return;

        if (tool.Qualities.Contains(door.PryingQuality))
        {
            args.Handled = TryPryDoor(uid, args.Used, args.User, door, out _);
        }
    }

    private void OnWeldAttempt(EntityUid uid, DoorComponent component, WeldableAttemptEvent args)
    {
        if (component.CurrentlyCrushing.Count > 0)
        {
            args.Cancel();
            return;
        }
        if (component.State != DoorState.Closed && component.State != DoorState.Welded)
        {
            args.Cancel();
        }
    }

    private void OnWeldChanged(EntityUid uid, DoorComponent component, ref WeldableChangedEvent args)
    {
        if (component.State == DoorState.Closed)
            SetState(uid, DoorState.Welded, component);
        else if (component.State == DoorState.Welded)
            SetState(uid, DoorState.Closed, component);
    }

    private void OnDoorAltVerb(EntityUid uid, DoorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<ToolComponent>(args.User, out var tool) || !tool.Qualities.Contains(component.PryingQuality))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("door-pry"),
            Impact = LogImpact.Low,
            Act = () => TryPryDoor(uid, args.User, args.User, component, out _, force: true),
        });
    }


    /// <summary>
    ///     Pry open a door. This does not check if the user is holding the required tool.
    /// </summary>
    public bool TryPryDoor(EntityUid target, EntityUid tool, EntityUid user, DoorComponent door, out DoAfterId? id, bool force = false)
    {
        id = null;

        if (door.State == DoorState.Welded)
            return false;

        if (!force)
        {
            var canEv = new BeforeDoorPryEvent(user, tool);
            RaiseLocalEvent(target, canEv, false);

            if (!door.CanPry || canEv.Cancelled)
                // mark handled, as airlock component will cancel after generating a pop-up & you don't want to pry a tile
                // under a windoor.
                return true;
        }

        var modEv = new DoorGetPryTimeModifierEvent(user);
        RaiseLocalEvent(target, modEv, false);

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool)} to pry {ToPrettyString(target)} while it is {door.State}"); // TODO move to generic tool use logging in a way that includes door state
        _toolSystem.UseTool(tool, user, target, TimeSpan.FromSeconds(modEv.PryTimeModifier * door.PryTime), new[] {door.PryingQuality}, new DoorPryDoAfterEvent(), out id);
        return true; // we might not actually succeeded, but a do-after has started
    }

    private void OnPryFinished(EntityUid uid, DoorComponent door, DoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (door.State == DoorState.Closed)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} open"); // TODO move to generic tool use logging in a way that includes door state
            StartOpening(uid, door);
        }
        else if (door.State == DoorState.Open)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} closed"); // TODO move to generic tool use logging in a way that includes door state
            StartClosing(uid, door);
        }
    }
#endregion


    /// <summary>
    ///     Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer
    ///     generate "access denied" sounds as you fire at a door.
    /// </summary>
    protected override void HandleCollide(EntityUid uid, DoorComponent door, ref StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        // Then this can be moved to the shared system without mispredicting.
        if (!door.BumpOpen)
            return;

        if (door.State != DoorState.Closed)
            return;

        var otherUid = args.OtherEntity;

        if (Tags.HasTag(otherUid, "DoorBumpOpener"))
            TryOpen(uid, door, otherUid);
    }
    private void OnEmagged(EntityUid uid, DoorComponent door, ref GotEmaggedEvent args)
    {
        if(TryComp<AirlockComponent>(uid, out var airlockComponent))
        {
            if (_bolts.IsBolted(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (door.State == DoorState.Closed)
            {
                SetState(uid, DoorState.Emagging, door);
                PlaySound(uid, door.SparkSound, AudioParams.Default.WithVolume(8), args.UserUid, false);
                args.Handled = true;
            }
        }
    }

    public override void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        var lastState = door.State;

        SetState(uid, DoorState.Opening, door);

        if (door.OpenSound != null)
            PlaySound(uid, door.OpenSound, AudioParams.Default.WithVolume(-5), user, predicted);

        if(lastState == DoorState.Emagging && TryComp<DoorBoltComponent>(uid, out var doorBoltComponent))
            _bolts.SetBoltsWithAudio(uid, doorBoltComponent, !doorBoltComponent.BoltsDown);
    }

    protected override void CheckDoorBump(DoorComponent component, PhysicsComponent body)
    {
        var uid = body.Owner;
        if (component.BumpOpen)
        {
            foreach (var other in PhysicsSystem.GetContactingEntities(uid, body, approximate: true))
            {
                if (Tags.HasTag(other, "DoorBumpOpener") && TryOpen(uid, component, other, false, quiet: true))
                    break;
            }
        }
    }
}

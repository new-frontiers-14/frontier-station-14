using System.Collections.Frozen;
using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Fax;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.Commands.Generic;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxShuffleMessage>(OnJukeboxShuffle);
        SubscribeLocalEvent<JukeboxComponent, JukeboxReplayMessage>(OnJukeboxReplay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (component.IsShuffleOn)
            {
                JukeboxPrototype randomSong =_protoManager.EnumeratePrototypes<JukeboxPrototype>()
                    .Skip(_random.Next(_protoManager.Count<JukeboxPrototype>()))
                    .First();

                component.SelectedSongId = randomSong.ID;
            }

            if (string.IsNullOrEmpty(component.SelectedSongId) ||
                !_protoManager.TryIndex(component.SelectedSongId, out var jukeboxProto))
            {
                return;
            }

            component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, AudioParams.Default.WithMaxDistance(10f))?.Entity;
            // Frontier: wallmount jukebox
            if (TryComp<TransformComponent>(component.AudioStream, out var xform))
            {
                xform.LocalPosition = component.AudioOffset;
            }
            // End Frontier
            Dirty(uid, component);
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnJukeboxShuffle(Entity<JukeboxComponent> ent, ref JukeboxShuffleMessage args)
    {
        ent.Comp.IsShuffleOn = !ent.Comp.IsShuffleOn;
        Logger.Warning($"JUKEBOX {ent.Owner} TRIED TO SHUFFLE. SETTING SHUFFLE TO {ent.Comp.IsShuffleOn}");
    }

    private void OnJukeboxReplay(Entity<JukeboxComponent> ent, ref JukeboxReplayMessage args)
    {
        ent.Comp.IsReplayOn = !ent.Comp.IsReplayOn;
        Logger.Warning($"JUKEBOX {ent.Owner} TRIED TO REPLAY. SETTING REPLAY TO {ent.Comp.IsReplayOn}");
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SelectedSongId = args.SongId;
            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.AudioStream = Audio.Stop(component.AudioStream);
        }

        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState(uid, comp);
                }
            }

            if (!Audio.IsPlaying(comp.AudioStream)  && comp.IsReplayOn)
            {
                var msg = new JukeboxPlayingMessage();
                OnJukeboxPlay(uid, comp, ref msg);
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        Logger.Warning($"{System.Reflection.MethodBase.GetCurrentMethod()!.Name}");
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }
}

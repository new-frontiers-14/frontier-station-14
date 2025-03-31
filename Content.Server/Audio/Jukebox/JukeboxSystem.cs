using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;
using System.Linq; // Frontier
using Robust.Shared.Random; // Frontier

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // Frontier
    [Dependency] private readonly IEntityManager _entityManager = default!; // Frontier
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxShuffleMessage>(OnJukeboxShuffle); // Frontier
        SubscribeLocalEvent<JukeboxComponent, JukeboxReplayMessage>(OnJukeboxReplay); // Frontier
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, ComponentStartup>(OnComponentStartup); // Frontier
        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }
    // Frontier: For Shuffle & Replay Buttons.
    private void OnComponentStartup<T>(Entity<JukeboxComponent> entity, ref T ev)
    {
        UpdateUIElements(entity);
    }

    private void UpdateUIElements(Entity<JukeboxComponent> entity)
    {
        var (owner, component) = entity;
        var state = new JukeboxInterfaceState(component.IsReplayOn, component.IsShuffleOn);
        _userInterface.SetUiState(owner, JukeboxUiKey.Key, state);
    }
    // End Frontier

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (string.IsNullOrEmpty(component.SelectedSongId) ||
                !_protoManager.TryIndex(component.SelectedSongId, out var jukeboxProto))
            {
                return;
            }

            // Frontier: Shuffling feature.
            if (component.IsShuffleOn && component.PrevSelectedSongId != null)
            {
                jukeboxProto =_protoManager.EnumeratePrototypes<JukeboxPrototype>()
                    .Skip(_random.Next(_protoManager.Count<JukeboxPrototype>()))
                    .First();

                component.SelectedSongId = jukeboxProto;
            }

            component.PrevSelectedSongId = component.SelectedSongId;
            // End Frontier
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
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }
    // Frontier: Event handlers.
    private void OnJukeboxShuffle(Entity<JukeboxComponent> ent, ref JukeboxShuffleMessage args)
    {
        ent.Comp.IsShuffleOn = !ent.Comp.IsShuffleOn;
        UpdateUIElements(ent);
        Dirty(ent);
    }

    private void OnJukeboxReplay(Entity<JukeboxComponent> ent, ref JukeboxReplayMessage args)
    {
        ent.Comp.IsReplayOn = !ent.Comp.IsReplayOn;
        UpdateUIElements(ent);
        Dirty(ent);
    }

    public AudioState GetAudioState(EntityUid? entity, AudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component, false))
        {
            return AudioState.Stopped; // I'm using this instead of nullifying the return value. It works.
        }

        return component.State;
    }
    // End Frontier

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }
    // Frontier: Modified Stop() function for the Shuffling & Replay features.
    private void Stop(Entity<JukeboxComponent> entity)
    {
        //Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped); // No longer needed since we're removing the AudioStream.
        entity.Comp.AudioStream = Audio.Stop(entity.Comp.AudioStream);
        entity.Comp.PrevSelectedSongId = null;
        Dirty(entity);
    }
    // End Frontier

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SelectedSongId = args.SongId;
            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.AudioStream = Audio.Stop(component.AudioStream);
            component.PrevSelectedSongId = null; // Frontier
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
            // Frontier: Replay feature. Please pitch in if you have better ideas. This is a pretty bad implementation.
            if (comp.IsReplayOn && comp.AudioStream != null &&
                GetAudioState(comp.AudioStream) != AudioState.Playing &&
                GetAudioState(comp.AudioStream) != AudioState.Paused)
            {
                var msg = new JukeboxPlayingMessage();
                OnJukeboxPlay(comp.Owner, comp, ref msg);
            }
            // End Frontier
        }
    }

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
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

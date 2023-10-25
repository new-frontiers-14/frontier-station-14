using System.Linq;
using System.Numerics;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Audio;
//TODO: This is using a incomplete version of the whole "only play nearest sounds" algo, that breaks down a bit should the ambient sound cap get hit.
//TODO: This'll be fixed when GetEntitiesInRange produces consistent outputs.

/// <summary>
/// Samples nearby <see cref="AmbientSoundComponent"/> and plays audio.
/// </summary>
public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
{
    [Dependency] private readonly AmbientSoundTreeSystem _treeSys = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void QueueUpdate(EntityUid uid, AmbientSoundComponent ambience)
        => _treeSys.QueueTreeUpdate(uid, ambience);

    private AmbientSoundOverlay? _overlay;
    private int _maxAmbientCount;
    private bool _overlayEnabled;
    private float _maxAmbientRange;
    private Vector2 MaxAmbientVector => new(_maxAmbientRange, _maxAmbientRange);

    private float _cooldown;
    private TimeSpan _targetTime = TimeSpan.Zero;
    private float _ambienceVolume = 0.0f;

    private static AudioParams _params = AudioParams.Default.WithVariation(0.01f).WithLoop(true).WithAttenuation(Attenuation.LinearDistance);

    /// <summary>
    /// How many times we can be playing 1 particular sound at once.
    /// </summary>
    private int MaxSingleSound => (int) (_maxAmbientCount / (16.0f / 6.0f));

    private readonly Dictionary<Entity<AmbientSoundComponent>, (IPlayingAudioStream? Stream, SoundSpecifier Sound, string Path)> _playingSounds = new();
    private readonly Dictionary<string, int> _playingCount = new();

    public bool OverlayEnabled
    {
        get => _overlayEnabled;
        set
        {
            if (_overlayEnabled == value) return;
            _overlayEnabled = value;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();

            if (_overlayEnabled)
            {
                _overlay = new AmbientSoundOverlay(EntityManager, this, EntityManager.System<EntityLookupSystem>());
                overlayManager.AddOverlay(_overlay);
            }
            else
            {
                overlayManager.RemoveOverlay(_overlay!);
                _overlay = null;
            }
        }
    }

    /// <summary>
    /// Is this AmbientSound actively playing right now?
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool IsActive(Entity<AmbientSoundComponent> component)
    {
        return _playingSounds.ContainsKey(component);
    }

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        UpdatesAfter.Add(typeof(AmbientSoundTreeSystem));

        _cfg.OnValueChanged(CCVars.AmbientCooldown, SetCooldown, true);
        _cfg.OnValueChanged(CCVars.MaxAmbientSources, SetAmbientCount, true);
        _cfg.OnValueChanged(CCVars.AmbientRange, SetAmbientRange, true);
        _cfg.OnValueChanged(CCVars.AmbienceVolume, SetAmbienceVolume, true);
        SubscribeLocalEvent<AmbientSoundComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, AmbientSoundComponent component, ComponentShutdown args)
    {
        if (!_playingSounds.Remove((uid, component), out var sound))
            return;

        sound.Stream?.Stop();
        _playingCount[sound.Path] -= 1;
        if (_playingCount[sound.Path] == 0)
            _playingCount.Remove(sound.Path);
    }

    private void SetAmbienceVolume(float value)
    {
        _ambienceVolume = value;

        foreach (var ((_, comp), values) in _playingSounds)
        {
            if (values.Stream == null)
                continue;

            var stream = (AudioSystem.PlayingStream) values.Stream;
            stream.Volume = _params.Volume + comp.Volume + _ambienceVolume;
        }
    }
    private void SetCooldown(float value) => _cooldown = value;
    private void SetAmbientCount(int value) => _maxAmbientCount = value;
    private void SetAmbientRange(float value) => _maxAmbientRange = value;

    public override void Shutdown()
    {
        base.Shutdown();
        ClearSounds();

        _cfg.UnsubValueChanged(CCVars.AmbientCooldown, SetCooldown);
        _cfg.UnsubValueChanged(CCVars.MaxAmbientSources, SetAmbientCount);
        _cfg.UnsubValueChanged(CCVars.AmbientRange, SetAmbientRange);
        _cfg.UnsubValueChanged(CCVars.AmbienceVolume, SetAmbienceVolume);
    }

    private int PlayingCount(string countSound)
    {
        var count = 0;

        foreach (var (_, (_, sound, path)) in _playingSounds)
        {
            if (path.Equals(countSound))
                count++;
        }

        return count;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (_cooldown <= 0f)
            return;

        if (_gameTiming.CurTime < _targetTime)
            return;

        _targetTime = _gameTiming.CurTime+TimeSpan.FromSeconds(_cooldown);

        var player = _playerManager.LocalPlayer?.ControlledEntity;
        if (!EntityManager.TryGetComponent(player, out TransformComponent? xform))
        {
            ClearSounds();
            return;
        }

        ProcessNearbyAmbience(xform);
    }

    private void ClearSounds()
    {
        foreach (var (stream, _, _) in _playingSounds.Values)
        {
            stream?.Stop();
        }

        _playingSounds.Clear();
        _playingCount.Clear();
    }

    private readonly struct QueryState
    {
        public readonly Dictionary<string, List<(float Importance, Entity<AmbientSoundComponent>)>> SourceDict = new();
        public readonly Vector2 MapPos;
        public readonly TransformComponent Player;
        public readonly EntityQuery<TransformComponent> Query;

        public QueryState(Vector2 mapPos, TransformComponent player, EntityQuery<TransformComponent> query)
        {
            MapPos = mapPos;
            Player = player;
            Query = query;
        }
    }

    private static bool Callback(
        ref QueryState state,
        in ComponentTreeEntry<AmbientSoundComponent> value)
    {
        var (ambientComp, xform) = value;

        DebugTools.Assert(ambientComp.Enabled);

        var delta = xform.ParentUid == state.Player.ParentUid
            ? xform.LocalPosition - state.Player.LocalPosition
            : xform.WorldPosition - state.MapPos;

        var range = delta.Length();
        if (range >= ambientComp.Range)
            return true;

        string key;

        if (ambientComp.Sound is SoundPathSpecifier path)
            key = path.Path.ToString();
        else
            key = ((SoundCollectionSpecifier) ambientComp.Sound).Collection ?? string.Empty;

        // Prioritize far away & loud sounds.
        var importance = range * (ambientComp.Volume + 32);
        state.SourceDict.GetOrNew(key).Add((importance, (ambientComp.Owner, ambientComp)));
        return true;
    }

    /// <summary>
    /// Get a list of ambient components in range and determine which ones to start playing.
    /// </summary>
    private void ProcessNearbyAmbience(TransformComponent playerXform)
    {
        var query = GetEntityQuery<TransformComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var mapPos = playerXform.MapPosition;

        // Remove out-of-range ambiences
        foreach (var (ent, sound) in _playingSounds)
        {
            var entity = ent.Owner;
            var comp = ent.Comp;

            if (comp.Enabled &&
                // Don't keep playing sounds that have changed since.
                sound.Sound == comp.Sound &&
                query.TryGetComponent(entity, out var xform) &&
                xform.MapID == playerXform.MapID &&
                !metaQuery.GetComponent(entity).EntityPaused)
            {
                var distance = (xform.ParentUid == playerXform.ParentUid)
                    ? xform.LocalPosition - playerXform.LocalPosition
                    : xform.WorldPosition - mapPos.Position;

                if (distance.LengthSquared() < comp.Range * comp.Range)
                    continue;
            }

            sound.Stream?.Stop();
            _playingSounds.Remove((entity, comp));
            _playingCount[sound.Path] -= 1;
            if (_playingCount[sound.Path] == 0)
                _playingCount.Remove(sound.Path);
        }

        if (_playingSounds.Count >= _maxAmbientCount)
            return;

        var pos = mapPos.Position;
        var state = new QueryState(pos, playerXform, query);
        var worldAabb = new Box2(pos - MaxAmbientVector, pos + MaxAmbientVector);
        _treeSys.QueryAabb(ref state, Callback, mapPos.MapId, worldAabb);

        // Add in range ambiences
        foreach (var (key, sources) in state.SourceDict)
        {
            if (_playingSounds.Count >= _maxAmbientCount)
                break;

            if (_playingCount.TryGetValue(key, out var playingCount) && playingCount >= MaxSingleSound)
                continue;

            sources.Sort(static (a, b) => b.Importance.CompareTo(a.Importance));

            foreach (var (_, ent) in sources)
            {
                var uid = ent.Owner;
                var comp = ent.Comp;

                if (_playingSounds.ContainsKey(ent) ||
                    metaQuery.GetComponent(uid).EntityPaused)
                    continue;

                var audioParams = _params
                    .AddVolume(comp.Volume + _ambienceVolume)
                    // Randomise start so 2 sources don't increase their volume.
                    .WithPlayOffset(_random.NextFloat(0.0f, 100.0f))
                    .WithMaxDistance(comp.Range);

                var stream = _audio.PlayPvs(comp.Sound, uid, audioParams);
                if (stream == null)
                    continue;

                _playingSounds[ent] = (stream, comp.Sound, key);
                playingCount++;

                if (_playingSounds.Count >= _maxAmbientCount)
                    break;
            }

            if (playingCount != 0)
                _playingCount[key] = playingCount;
        }

        DebugTools.Assert(_playingCount.All(x => x.Value == PlayingCount(x.Key)));
    }
}

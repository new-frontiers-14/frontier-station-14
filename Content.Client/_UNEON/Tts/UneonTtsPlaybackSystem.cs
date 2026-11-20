using System.IO;
using System.Linq;
using Content.Shared._UNEON.Tts;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;

namespace Content.Client._UNEON.Tts;

public sealed class UneonTtsPlaybackSystem : EntitySystem
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly Dictionary<EntityUid, AudioStream> _playing = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<UneonTtsAudioEvent>(OnAudio);
    }

    public void Preview(string voice) => RaiseNetworkEvent(new UneonTtsPreviewEvent(voice));

    private void OnAudio(UneonTtsAudioEvent message)
    {
        if (!_cfg.GetCVar(UneonTts.Enabled) || _playing.Count >= 12 ||
            message.Audio.Length is 0 or > UneonTts.MaxAudioBytes)
            return;

        if (!TryGetEntity(message.Source, out var source) && message.Source != null)
            return;

        AudioStream? stream = null;
        try
        {
            using var input = new MemoryStream(message.Audio, writable: false);
            stream = _audioManager.LoadAudioOggVorbis(input);
            var parameters = AudioParams.Default.WithVolume(-10f).WithMaxDistance(SharedChatSystem.VoiceRange);
            var playing = source is { } uid
                ? _audio.PlayEntity(stream, uid, null, parameters)
                : _audio.PlayGlobal(stream, null, parameters);
            if (playing == null)
                return;

            _playing.Add(playing.Value.Entity, stream);
            stream = null;
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not play UNEON speech audio ({ex.GetType().Name}).");
        }
        finally
        {
            stream?.Dispose();
        }
    }

    public override void Update(float frameTime)
    {
        if (_playing.Count == 0)
            return;

        foreach (var (uid, stream) in _playing.ToArray())
        {
            if (TryComp<AudioComponent>(uid, out var audio) && audio.Playing)
                continue;

            _playing.Remove(uid);
            stream.Dispose();
        }
    }

    public override void Shutdown()
    {
        foreach (var stream in _playing.Values)
            stream.Dispose();
        _playing.Clear();
        base.Shutdown();
    }
}

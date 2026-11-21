using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._UNEON.Tts;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using NVorbis;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._UNEON.Tts;

// HTTP and positional playback approach adapted from frontier-Radiant's Corvax TTS integration.
// https://github.com/TetrisZz/frontier-Radiant/tree/master/Content.Server/Corvax/TTS
public sealed class UneonTtsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    private readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = false });
    private readonly Dictionary<(string Voice, string Text), byte[]> _cache = new();
    private readonly Queue<(string Voice, string Text)> _cachedOrder = new();
    private readonly Dictionary<(string Voice, string Text), Task<byte[]?>> _pending = new();
    private readonly SemaphoreSlim _serial = new(1);
    private readonly Dictionary<NetUserId, long> _nextPreview = new();
    private long _nextRequest;
    private int _round;
    public bool Enabled => _cfg.GetCVar(UneonTts.Enabled);

    public override void Initialize()
    {
        SubscribeNetworkEvent<UneonTtsPreviewEvent>(OnPreview);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            _round++;
            _nextPreview.Clear();
        });
    }

    public override void Shutdown()
    {
        _round++;
        _client.Dispose();
        base.Shutdown();
    }

    public async void Speak(EntityUid source, string message, List<ICommonSession> listeners)
    {
        if (listeners.Count == 0)
            return;

        var recipients = listeners.Select(session => (Session: session, Entity: session.AttachedEntity)).ToArray();
        var audio = await GenerateSpeech(source, message);
        if (audio == null ||
            !TryComp<TransformComponent>(source, out var emitter) || TerminatingOrDeleted(source))
            return;

        foreach (var (session, entity) in recipients)
        {
            if (session.Status != SessionStatus.InGame || session.AttachedEntity != entity ||
                !TryComp<TransformComponent>(entity, out var listener) ||
                !emitter.Coordinates.TryDistance(EntityManager, listener.Coordinates, out var distance) ||
                distance >= SharedChatSystem.VoiceRange)
                continue;

            RaiseNetworkEvent(new UneonTtsAudioEvent(audio, GetNetEntity(source)), Filter.SinglePlayer(session), recordReplay: false);
        }
    }

    public async void SpeakRadio(EntityUid source, string message, HashSet<ICommonSession> listeners)
    {
        if (listeners.Count == 0)
            return;

        var recipients = listeners.Select(session => (Session: session, Entity: session.AttachedEntity)).ToArray();
        var audio = await GenerateSpeech(source, message);
        if (audio == null)
            return;

        var radio = FilterRadioAudio(audio);
        foreach (var (session, entity) in recipients)
        {
            if (session.Status == SessionStatus.InGame && session.AttachedEntity == entity)
                RaiseNetworkEvent(new UneonTtsAudioEvent(radio ?? audio, null, radio: radio != null), Filter.SinglePlayer(session), recordReplay: false);
        }
    }

    private byte[]? FilterRadioAudio(byte[] audio)
    {
        try
        {
            using var stream = new MemoryStream(audio, writable: false);
            using var reader = new VorbisReader(stream, false);
            reader.Initialize();
            var rate = reader.SampleRate;
            var channels = reader.Channels;
            var frames = reader.TotalSamples;
            if (rate is < UneonTts.RadioSampleRate or > 96000 || channels is < 1 or > 2 ||
                frames <= 0 || frames > (long) UneonTts.MaxAudioBytes * rate / UneonTts.RadioSampleRate)
                return null;

            var output = new byte[(int) (frames * UneonTts.RadioSampleRate / rate)];
            Span<float> buffer = stackalloc float[2048];
            var highPass = MathF.Exp(-2f * MathF.PI * 350f / rate);
            var lowPass = 1f - MathF.Exp(-2f * MathF.PI * 2600f / rate);
            float previous = 0f, high = 0f, band = 0f;
            var phase = 0;
            var written = 0;
            while (frames > 0)
            {
                var requested = (int) Math.Min(frames, buffer.Length / channels);
                var read = reader.ReadSamples(buffer[..(requested * channels)]);
                if (read == 0)
                    break;

                frames -= read;
                for (var frame = 0; frame < read; frame++)
                {
                    var index = frame * channels;
                    var sample = channels == 1 ? buffer[index] : (buffer[index] + buffer[index + 1]) * 0.5f;
                    high = highPass * (high + sample - previous);
                    previous = sample;
                    band += lowPass * (high - band);
                    phase += UneonTts.RadioSampleRate;
                    if (phase < rate)
                        continue;

                    phase -= rate;
                    output[written++] = (byte) (128f + 127f * MathF.Tanh(2f * band));
                }
            }

            return written == 0 ? null : output[..written];
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not apply UNEON radio audio filter ({ex.GetType().Name}).");
            return null;
        }
    }

    private async Task<byte[]?> GenerateSpeech(EntityUid source, string message)
    {
        if (!Enabled || message.Length is 0 or > 200 ||
            !TryComp<HumanoidAppearanceComponent>(source, out var humanoid) ||
            humanoid.TtsVoice is not { } voice || !_prototypes.HasIndex<UneonTtsVoicePrototype>(voice))
            return null;

        var round = _round;
        var audio = await Generate(voice, message);
        return round == _round && Enabled ? audio : null;
    }

    private async void OnPreview(UneonTtsPreviewEvent message, EntitySessionEventArgs args)
    {
        if (!_cfg.GetCVar(UneonTts.Enabled) || string.IsNullOrEmpty(message.Voice) ||
            !_prototypes.HasIndex<UneonTtsVoicePrototype>(message.Voice))
            return;

        var session = args.SenderSession;
        var now = Environment.TickCount64;
        if (_nextPreview.TryGetValue(session.UserId, out var next) && now < next)
            return;

        _nextPreview[session.UserId] = now + 2000;
        var round = _round;
        var audio = await Generate(message.Voice, "Привет! Так звучит мой голос.");
        if (audio != null && round == _round && _cfg.GetCVar(UneonTts.Enabled) &&
            session.Status is SessionStatus.InGame or SessionStatus.Connected)
            RaiseNetworkEvent(new UneonTtsAudioEvent(audio, null), Filter.SinglePlayer(session), recordReplay: false);
    }

    private async Task<byte[]?> Generate(string voice, string text)
    {
        var key = (voice, text);
        if (_cache.TryGetValue(key, out var audio))
            return audio;
        if (_pending.TryGetValue(key, out var pending))
            return await pending;

        var token = _cfg.GetCVar(UneonTts.ApiToken);
        if (string.IsNullOrWhiteSpace(token) || _pending.Count >= 3)
            return null;

        var request = RequestAudio(voice, text, token, _round);
        _pending.Add(key, request);
        try
        {
            audio = await request;
            if (audio == null)
                return null;

            _cache.Add(key, audio);
            _cachedOrder.Enqueue(key);
            if (_cachedOrder.Count > 64)
                _cache.Remove(_cachedOrder.Dequeue());
            return audio;
        }
        finally
        {
            _pending.Remove(key);
        }
    }

    private async Task<byte[]?> RequestAudio(string voice, string text, string token, int round)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var acquired = false;
        try
        {
            await _serial.WaitAsync(timeout.Token);
            acquired = true;
            var delay = _nextRequest - Environment.TickCount64;
            if (delay > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(delay), timeout.Token);
            if (round != _round || !_cfg.GetCVar(UneonTts.Enabled))
                return null;

            _nextRequest = Environment.TickCount64 + 1000;
            var url = $"https://ntts.fdev.team/api/v1/tts?speaker={Uri.EscapeDataString(voice)}&text={Uri.EscapeDataString(text)}&ext=ogg";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > UneonTts.MaxAudioBytes)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var result = new MemoryStream();
            var buffer = new byte[4096];
            int count;
            while ((count = await stream.ReadAsync(buffer, timeout.Token)) > 0)
            {
                if (result.Length + count > UneonTts.MaxAudioBytes)
                    return null;
                result.Write(buffer, 0, count);
            }

            var bytes = result.ToArray();
            return IsVorbis(bytes) ? bytes : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or FormatException or ObjectDisposedException)
        {
            return null;
        }
        finally
        {
            if (acquired)
                _serial.Release();
        }
    }

    private static bool IsVorbis(byte[] bytes) =>
        bytes.AsSpan().StartsWith("OggS"u8) &&
        bytes.AsSpan(0, Math.Min(bytes.Length, 256)).IndexOf("\u0001vorbis"u8) >= 0;
}

using Robust.Shared.Configuration;
using Robust.Shared.Serialization;

namespace Content.Shared._UNEON.Tts;

[CVarDefs]
public sealed class UneonTts
{
    public const int MaxAudioBytes = 128 * 1024;
    public const int RadioSampleRate = 8000;

    public static readonly CVarDef<bool> Enabled = CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<string> ApiToken = CVarDef.Create("tts.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

[Serializable, NetSerializable]
public sealed class UneonTtsPreviewEvent(string voice) : EntityEventArgs
{
    public string Voice { get; } = voice;
}

[Serializable, NetSerializable]
public sealed class UneonTtsAudioEvent(byte[] audio, NetEntity? source, bool radio = false) : EntityEventArgs
{
    public byte[] Audio { get; } = audio;
    public NetEntity? Source { get; } = source;
    public bool Radio { get; } = radio;
}

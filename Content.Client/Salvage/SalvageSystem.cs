using Content.Client.Audio;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Content.Shared._NF.CCVar; // Frontier
using Robust.Client.Audio; // Frontier
using Robust.Shared.Audio; // Frontier
using Robust.Shared.Configuration; // Frontier
using Robust.Shared.Player; // Frontier

namespace Content.Client.Salvage;

public sealed class SalvageSystem : SharedSalvageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ContentAudioSystem _audio = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!; // Frontier
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Frontier

    const float SalvageExpeditionMinMusicVolume = -30f; // Frontier: expedition volume range
    const float SalvageExpeditionMaxMusicVolume = 3.0f; // Frontier: expedition volume range

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentHandleState>(OnExpeditionHandleState);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentRemove>(OnRemove); // Frontier
        Subs.CVar(_cfg, NFCCVars.SalvageExpeditionMusicVolume, SetMusicVolume); // Frontier
    }

    private void OnExpeditionHandleState(EntityUid uid, SalvageExpeditionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SalvageExpeditionComponentState state)
            return;

        component.Stage = state.Stage;
        if (state.SelectedSong != null) // Frontier
            component.SelectedSong = state.SelectedSong; // Frontier

        if (component.Stage >= ExpeditionStage.MusicCountdown)
        {
            _audio.DisableAmbientMusic();
        }

        // Frontier: add music (only on music countdown, no music on forced exit)
        if (component.Stage == ExpeditionStage.MusicCountdown
            && component.SelectedSong != null
            && component.Stream == null)
        {
            var volume = ConvertSliderValueToVolume(_cfg.GetCVar(NFCCVars.SalvageExpeditionMusicVolume));
            var audioParams = AudioParams.Default.WithVolume(volume);
            var audio = _audioSystem.PlayEntity(component.SelectedSong, Filter.Local(), uid, false, audioParams);
            _audioSystem.SetMapAudio(audio);

            component.Stream = audio?.Entity;
        }
        // End Frontier
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent ev)
    {
        if (ev.Cancelled)
            return;

        var player = _playerManager.LocalEntity;

        if (!TryComp(player, out TransformComponent? xform) ||
            !TryComp<SalvageExpeditionComponent>(xform.MapUid, out var expedition) ||
            expedition.Stage < ExpeditionStage.MusicCountdown)
        {
            return;
        }

        ev.Cancelled = true;
    }

    // Frontier: stop stream when destroying the expedition
    private void OnRemove(EntityUid uid, SalvageExpeditionComponent component, ComponentRemove args)
    {
        // For whatever reason, this stream is considered a server-side entity, so the AudioSystem won't tear it down.
        // Don't really understand why, but I don't think it is.

        //component.Stream = _audioSystem.Stop(component.Stream);
        QueueDel(component.Stream);
    }

    private void SetMusicVolume(float volume)
    {
        var expedQuery = EntityQueryEnumerator<SalvageExpeditionComponent>();
        while (expedQuery.MoveNext(out _, out var comp))
        {
            if (comp.Stream != null)
                _audioSystem.SetVolume(comp.Stream, ConvertSliderValueToVolume(volume));
        }
    }

    private float ConvertSliderValueToVolume(float value)
    {
        var ret = AudioSystem.GainToVolume(value);
        if (!float.IsFinite(ret)) // Explicitly handle any odd cases (chiefly NaN)
            ret = SalvageExpeditionMinMusicVolume;
        else
            ret = Math.Clamp(ret, SalvageExpeditionMinMusicVolume, SalvageExpeditionMaxMusicVolume);
        return ret;
    }
    // End Frontier: stop stream when destroying the expedition
}

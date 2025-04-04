using Content.Client.Audio;
using Content.Shared._NF.CCVar; // Frontier
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Robust.Client.Audio; // Frontier
using Robust.Client.Player;
using Robust.Shared.Audio; // Frontier
using Robust.Shared.Configuration; // Frontier
using Robust.Shared.GameStates;

namespace Content.Client.Salvage;

public sealed partial class SalvageSystem : SharedSalvageSystem // Frontier: added partial (see SalvageSystem.Audio.cs)
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ContentAudioSystem _audio = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!; // Frontier
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Frontier

    const float SalvageExpeditionMusicVolumeOffset = -30f; // Frontier: constant to add to expedition volume

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentHandleState>(OnExpeditionHandleState);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentShutdown>(OnShutdown); // Frontier
        Subs.CVar(_cfg, NFCCVars.SalvageExpeditionMusicVolume, SetMusicVolume);
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
            var volume = _cfg.GetCVar(NFCCVars.SalvageExpeditionMusicVolume) + SalvageExpeditionMusicVolumeOffset;
            var audioParams = AudioParams.Default.WithVolume(volume);
            var audio = _audioSystem.PlayPvs(component.SelectedSong, uid, audioParams);
            component.Stream = audio?.Entity;
            _audioSystem.SetMapAudio(audio);
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
    private void OnShutdown(EntityUid uid, SalvageExpeditionComponent component, ComponentShutdown args)
    {
        component.Stream = _audioSystem.Stop(component.Stream); // Frontier: moved to client
    }

    private void SetMusicVolume(float volume)
    {
        var expedQuery = EntityQueryEnumerator<SalvageExpeditionComponent>();
        while (expedQuery.MoveNext(out _, out var comp))
        {
            if (comp.Stream != null)
                _audioSystem.SetVolume(comp.Stream, volume + SalvageExpeditionMusicVolumeOffset);
        }
    }
    // End Frontier: stop stream when destroying the expedition
}

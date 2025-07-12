using System.Numerics;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Radio.EntitySystems;
using Content.Shared._NF.CCVar;
using Content.Shared.Radio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking; // Intentionally colliding namespaces to extend the class

public sealed partial class GameTicker
{
    [Dependency] private readonly PlayTimeTrackingManager _playTimeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    private bool _newPlayerGreetingEnabled = true;
    private TimeSpan _newPlayerGreetingMaxTime = TimeSpan.FromMinutes(180);
    private ProtoId<RadioChannelPrototype> _newPlayerRadioChannel = "Service";
    private EntProtoId _greetingRadioSource = "GreetingRadioSource";
    private EntityUid _greetingEntity = EntityUid.Invalid;

    public void NFInitialize()
    {
        Subs.CVar(_cfg, NFCCVars.NewPlayerRadioGreetingEnabled, e => _newPlayerGreetingEnabled = e, true);
        Subs.CVar(_cfg, NFCCVars.NewPlayerRadioGreetingMaxPlaytime, e => _newPlayerGreetingMaxTime = TimeSpan.FromMinutes(e), true);
        Subs.CVar(_cfg, NFCCVars.NewPlayerRadioGreetingChannel, SetChannel, true);
    }

    private void SetChannel(string channel)
    {
        if (_prototypeManager.HasIndex<RadioChannelPrototype>(channel))
            _newPlayerRadioChannel = channel;
    }

    private void NFRoundStarted()
    {
        _greetingEntity = Spawn(_greetingRadioSource, new MapCoordinates(Vector2.Zero, DefaultMap));
    }

    private void NFRoundRestartCleanup()
    {
        if (_greetingEntity != EntityUid.Invalid)
        {
            QueueDel(_greetingEntity);
            _greetingEntity = EntityUid.Invalid;
        }
    }

    private void HandleGreetingMessage(ICommonSession session, EntityUid mob, EntityUid station)
    {
        if (!_newPlayerGreetingEnabled)
            return;

        TimeSpan playtime;
        try
        {
            playtime = _playTimeManager.GetOverallPlaytime(session);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (playtime < _newPlayerGreetingMaxTime)
        {
            _radio.SendRadioMessage(_greetingEntity, Loc.GetString("latejoin-arrival-new-player-announcement",
                    ("character", MetaData(mob).EntityName),
                    ("station", station)),
                    _newPlayerRadioChannel,
                    _greetingEntity);
        }
    }
}

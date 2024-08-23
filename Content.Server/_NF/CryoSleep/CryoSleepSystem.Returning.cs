using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Bed.Sleep;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared._NF.CCVar;
using Content.Shared.Players;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.CryoSleep;

public sealed partial class CryoSleepSystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private void InitReturning()
    {
        SubscribeNetworkEvent<WakeupRequestMessage>(OnWakeupMessage);
        SubscribeNetworkEvent<GetStatusMessage>(OnGetStatusMessage);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(e => ResetCryosleepState(e.PlayerSession.UserId));
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(e => ResetCryosleepState(e.Player.UserId));
    }

    private void OnWakeupMessage(WakeupRequestMessage message, EntitySessionEventArgs session)
    {
        var entity = session.SenderSession.GetMind();

        var result = entity == null || !TryComp<MindComponent>(entity, out var mind)
            ? ReturnToBodyStatus.NotAGhost
            : TryReturnToBody(mind);

        var msg = new WakeupRequestMessage.Response(result);
        RaiseNetworkEvent(msg, session.SenderSession);
    }

    public void OnGetStatusMessage(GetStatusMessage message, EntitySessionEventArgs args)
    {
        var msg = new GetStatusMessage.Response(HasCryosleepingBody(args.SenderSession.UserId));
        RaiseNetworkEvent(msg, args.SenderSession);
    }

    /// <summary>
    ///   Returns the mind to the original body, if any. The mind must be possessing a ghost, unless [force] is true.
    /// </summary>
    public ReturnToBodyStatus TryReturnToBody(MindComponent mind, bool force = false)
    {
        if (!_configurationManager.GetCVar(NFCCVars.CryoReturnEnabled))
            return ReturnToBodyStatus.Disabled;

        var id = mind.UserId;
        if (id == null || !_storedBodies.TryGetValue(id.Value, out var storedBody))
            return ReturnToBodyStatus.BodyMissing;

        if (!force && (mind.CurrentEntity is not { Valid: true } ghost || !HasComp<GhostComponent>(ghost)))
            return ReturnToBodyStatus.NotAGhost;

        var cryopod = storedBody!.Value.Cryopod;
        if (!Exists(cryopod) || Deleted(cryopod) || !TryComp<CryoSleepComponent>(cryopod, out var cryoComp))
            return ReturnToBodyStatus.CryopodMissing;

        var body = storedBody.Value.Body;
        if (IsOccupied(cryoComp) || !_container.Insert(body, cryoComp.BodyContainer))
            return ReturnToBodyStatus.Occupied;

        _storedBodies.Remove(id.Value);
        _mind.ControlMob(id.Value, body);
        // Force the mob to sleep
        var sleep = EnsureComp<SleepingComponent>(body);
        sleep.CooldownEnd = TimeSpan.FromSeconds(5);

        _popup.PopupEntity(Loc.GetString("cryopod-wake-up", ("entity", body)), body);

        RaiseLocalEvent(body, new CryosleepWakeUpEvent(storedBody.Value.Cryopod, id), true);

        _adminLogger.Add(LogType.LateJoin, LogImpact.Medium, $"{id.Value} has returned from cryosleep!");
        return ReturnToBodyStatus.Success;
    }

    /// <summary>
    ///   Removes the body of the given user from the cryosleep dictionary, making them unable to return to it.
    ///   Also actually deletes the body if it's still on that map.
    /// </summary>
    public void ResetCryosleepState(NetUserId id)
    {
        var body = _storedBodies.GetValueOrDefault(id, null);

        if (body != null && _storedBodies.Remove(id) && Transform(body!.Value.Body).ParentUid == _storageMap)
        {
            QueueDel(body.Value.Body);
        }
    }

    public bool HasCryosleepingBody(NetUserId id)
    {
        return _storedBodies.ContainsKey(id);
    }
}

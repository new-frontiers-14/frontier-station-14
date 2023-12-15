using Content.Shared.Bed.Sleep;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Network;

namespace Content.Server.CryoSleep;

public sealed partial class CryoSleepSystem
{
    private void InitReturning()
    {
        SubscribeNetworkEvent<WakeupRequestMessage>(OnWakeupMessage);
    }

    private void OnWakeupMessage(WakeupRequestMessage message, EntitySessionEventArgs session)
    {
        var entity = session.SenderSession.GetMind();

        var result = entity == null || !TryComp<MindComponent>(entity, out var mind)
            ? ReturnToBodyResult.NotAGhost
            : TryReturnToBody(mind);

        var msg = new WakeupRequestMessage.Response(result);
        RaiseNetworkEvent(msg, session.SenderSession);
    }

    /// <summary>
    ///   Returns the mind to the original body, if any. The mind must be possessing a ghost, unless [force] is true.
    /// </summary>
    public ReturnToBodyResult TryReturnToBody(MindComponent mind, bool force = false)
    {
        var id = mind.UserId;
        if (id == null || !_storedBodies.TryGetValue(id.Value, out var storedBody))
            return ReturnToBodyResult.BodyMissing;

        if (!force && (mind.CurrentEntity is not { Valid: true } ghost || !HasComp<GhostComponent>(ghost)))
            return ReturnToBodyResult.NotAGhost;

        var cryopod = storedBody!.Value.Cryopod;
        if (!Exists(cryopod) || Deleted(cryopod) || !TryComp<CryoSleepComponent>(cryopod, out var cryoComp))
            return ReturnToBodyResult.CryopodMissing;

        var body = storedBody.Value.Body;
        if (IsOccupied(cryoComp) || !cryoComp.BodyContainer.Insert(body, EntityManager))
            return ReturnToBodyResult.Occupied;

        _storedBodies.Remove(id.Value);
        _mind.ControlMob(id.Value, body);
        // Force the mob to sleep
        var sleep = EnsureComp<SleepingComponent>(body);
        sleep.CoolDownEnd = TimeSpan.FromSeconds(5);

        _popup.PopupEntity(Loc.GetString("cryopod-wake-up", ("entity", body)), body);

        return ReturnToBodyResult.Success;
    }

    /// <summary>
    ///   Removes the body of the given user from the cryosleep dictionary, making them unable to return to it.
    /// </summary>
    public void ResetCryosleepState(NetUserId id)
    {
        _storedBodies.Remove(id);
    }

    public bool HasCryosleepingBody(NetUserId id)
    {
        return _storedBodies.ContainsKey(id);
    }
}

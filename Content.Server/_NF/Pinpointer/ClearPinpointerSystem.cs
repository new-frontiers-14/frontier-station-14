using Content.Server.Pinpointer;
using Content.Shared._NF.Pinpointer;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;

namespace Content.Server._NF.Pinpointer;

public sealed class ClearPinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClearPinpointerComponent, AfterInteractEvent>(OnInteractUsing);
        SubscribeLocalEvent<ClearPinpointerComponent, ClearPinpointerDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<ClearPinpointerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target == null)
            return;

        if (ent.Comp.OtherMessage != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.OtherMessage, ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, args.Target.Value, PopupType.Large);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ClearTime, new ClearPinpointerDoAfterEvent(), ent, target: args.Target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }

    /// <summary>
    /// Prevent removing the tape cassette while the recorder is active
    /// </summary>
    private void OnDoAfter(Entity<ClearPinpointerComponent> ent, ref ClearPinpointerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target != null)
        {
            var pinpointers = EntityQueryEnumerator<PinpointerComponent>();
            while (pinpointers.MoveNext(out var pinpointerUid, out var pinpointerComp))
            {
                if (pinpointerComp.Target == args.Target)
                    _pinpointer.ClearPinpointer(pinpointerUid, pinpointerComp);
            }
        }

        QueueDel(ent.Owner);
    }
}

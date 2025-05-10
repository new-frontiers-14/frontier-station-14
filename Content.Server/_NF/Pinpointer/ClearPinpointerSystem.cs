using Content.Server.Charges.Systems;
using Content.Server.Pinpointer;
using Content.Shared._NF.Pinpointer;
using Content.Shared.Charges.Components;
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
    [Dependency] private readonly ChargesSystem _charges = default!;

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

        TryComp<LimitedChargesComponent>(ent, out var charges);
        if (_charges.IsEmpty(ent, charges))
        {
            if (ent.Comp.EmptyMessage != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.EmptyMessage), args.User, args.User);

            return;
        }

        if (args.User == args.Target)
        {
            if (ent.Comp.UseOnSelfMessage != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.UseOnSelfMessage, ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, args.Target.Value, PopupType.Small);
        }
        else
        {
            if (ent.Comp.UseOnOthersMessage != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.UseOnOthersMessage, ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, args.Target.Value, PopupType.Large);
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ClearTime, new ClearPinpointerDoAfterEvent(), ent, target: args.Target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }

    /// <summary>
    /// DoAfter: remove all pinpointers that point to this object
    /// </summary>
    private void OnDoAfter(Entity<ClearPinpointerComponent> ent, ref ClearPinpointerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryComp<LimitedChargesComponent>(ent, out var charges);
        if (_charges.IsEmpty(ent, charges))
        {
            if (ent.Comp.EmptyMessage != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.EmptyMessage), args.User, args.User);

            return;
        }

        if (TryComp<PinpointerTargetComponent>(args.Target, out var target))
        {
            foreach (var pinpointer in target.Entities)
            {
                if (!TryComp<PinpointerComponent>(pinpointer, out var pinpointComp))
                    continue;

                _pinpointer.ClearPinpointer(pinpointer, pinpointComp);
            }
            RemComp<PinpointerTargetComponent>(args.Target.Value);
        }

        if (charges != null)
            _charges.UseCharge(ent, charges);

        if (ent.Comp.DestroyAfterUse)
            QueueDel(ent.Owner);
    }
}

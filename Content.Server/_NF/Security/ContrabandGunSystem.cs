using Content.Server.Popups;
using Content.Shared._NF.Contraband.Components;
using Content.Server._NF.Security.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;

namespace Content.Server._NF.Security.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ContrabandGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ContrabandGunComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnUtilityVerb(EntityUid uid, ContrabandGunComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryComp<ContrabandComponent>(args.Target, out var contraband))
            return;

        // Calc contraband points
        var price = contraband.Value;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", Identity.Entity(args.Target, EntityManager)), ("price", $"{price:F2}")), args.User, args.User);
                _useDelay.TryResetDelay((uid, useDelay));
            },
            Text = Loc.GetString("price-gun-verb-text"),
            Message = Loc.GetString("price-gun-verb-message", ("object", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(EntityUid uid, ContrabandGunComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (TryComp<ContrabandComponent>(args.Target, out var contraband))
        {
            //double price = _pricingSystem.GetPrice(args.Target.Value);
            double price = contraband.Value;
            _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", Identity.Entity(args.Target.Value, EntityManager)), ("price", $"{price:F2}")), args.User, args.User);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("price-gun-bounty-complete"), args.User, args.User);
        }

        _useDelay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }
}

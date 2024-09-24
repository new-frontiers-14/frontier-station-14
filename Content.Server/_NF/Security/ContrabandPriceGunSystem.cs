using Content.Server.Popups;
using Content.Shared.Contraband;
using Content.Server._NF.Security.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using System.Linq;

namespace Content.Server._NF.Security.Systems;

/// <summary>
/// This system handles contraband appraisal messages and will inform a user of how much an item is worth for trade-in in FUCs.
/// </summary>
public sealed class ContrabandPriceGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandPriceGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ContrabandPriceGunComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnUtilityVerb(EntityUid uid, ContrabandPriceGunComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryComp<ContrabandComponent>(args.Target, out var contraband) || !contraband.TurnInValues.ContainsKey(component.Currency))
            return;
        
        var price = contraband.TurnInValues[component.Currency];

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                _popupSystem.PopupEntity(Loc.GetString($"{component.LocStringPrefix}contraband-price-gun-pricing-result", ("object", Identity.Entity(args.Target, EntityManager)), ("price", price)), args.User, args.User);
                _useDelay.TryResetDelay((uid, useDelay));
            },
            Text = Loc.GetString($"{component.LocStringPrefix}contraband-price-gun-verb-text"),
            Message = Loc.GetString($"{component.LocStringPrefix}contraband-price-gun-verb-message", ("object", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(EntityUid uid, ContrabandPriceGunComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (TryComp<ContrabandComponent>(args.Target, out var contraband) && contraband.TurnInValues.ContainsKey(component.Currency))
            _popupSystem.PopupEntity(Loc.GetString($"{component.LocStringPrefix}contraband-price-gun-pricing-result", ("object", Identity.Entity(args.Target.Value, EntityManager)), ("price", contraband.TurnInValues[component.Currency])), args.User, args.User);
        else
            _popupSystem.PopupEntity(Loc.GetString($"{component.LocStringPrefix}contraband-price-gun-pricing-result-none", ("object", Identity.Entity(args.Target.Value, EntityManager))), args.User, args.User);

        _useDelay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }
}

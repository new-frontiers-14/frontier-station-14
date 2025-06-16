using Content.Server.Popups;
using Content.Shared.Contraband;
using Content.Server._NF.Security.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Server._NF.Security.Systems;

/// <summary>
/// This system handles contraband appraisal messages and will inform a user of how much an item is worth for trade-in in FUCs.
/// </summary>
public sealed class ContrabandPriceGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandPriceGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ContrabandPriceGunComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnUtilityVerb(Entity<ContrabandPriceGunComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(entity, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((entity, useDelay)))
            return;

        if (!TryComp<ContrabandComponent>(args.Target, out var contraband) || !contraband.TurnInValues.ContainsKey(entity.Comp.Currency))
            return;

        var price = contraband.TurnInValues[entity.Comp.Currency];
        var user = args.User;
        var target = args.Target;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                _popupSystem.PopupEntity(Loc.GetString($"{entity.Comp.LocStringPrefix}contraband-price-gun-pricing-result", ("object", Identity.Entity(target, EntityManager)), ("price", price)), user, user);
                _useDelay.TryResetDelay((entity.Owner, useDelay));
            },
            Text = Loc.GetString($"{entity.Comp.LocStringPrefix}contraband-price-gun-verb-text"),
            Message = Loc.GetString($"{entity.Comp.LocStringPrefix}contraband-price-gun-verb-message", ("object", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(Entity<ContrabandPriceGunComponent> entity, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!TryComp(entity, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((entity, useDelay)))
            return;

        if (TryComp<ContrabandComponent>(args.Target, out var contraband) && contraband.TurnInValues.ContainsKey(entity.Comp.Currency))
            _popupSystem.PopupEntity(Loc.GetString($"{entity.Comp.LocStringPrefix}contraband-price-gun-pricing-result", ("object", Identity.Entity(args.Target.Value, EntityManager)), ("price", contraband.TurnInValues[entity.Comp.Currency])), args.User, args.User);
        else
            _popupSystem.PopupEntity(Loc.GetString($"{entity.Comp.LocStringPrefix}contraband-price-gun-pricing-result-none", ("object", Identity.Entity(args.Target.Value, EntityManager))), args.User, args.User);

        _audio.PlayPvs(entity.Comp.AppraisalSound, entity.Owner);
        _useDelay.TryResetDelay((entity, useDelay));
        args.Handled = true;
    }
}

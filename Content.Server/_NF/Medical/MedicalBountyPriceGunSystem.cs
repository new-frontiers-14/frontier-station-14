using Content.Server._NF.Medical.Components;
using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Shared._NF.Bank;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;

namespace Content.Server._NF.Medical.Systems;

/// <summary>
/// This checks the value of medical bounties on entities that might have them.
/// </summary>
public sealed class MedicalBountyPriceGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MedicalPriceGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedicalPriceGunComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnUtilityVerb(EntityUid uid, MedicalPriceGunComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                AppraiseEntity(args.Target, args.User);
                _useDelay.TryResetDelay((uid, useDelay));
            },
            Text = Loc.GetString("medical-price-gun-verb-text"),
            Message = Loc.GetString("medical-price-gun-verb-message", ("object", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(EntityUid uid, MedicalPriceGunComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        AppraiseEntity(args.Target.Value, args.User);
        _useDelay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }

    private void AppraiseEntity(EntityUid target, EntityUid user)
    {
        if (TryComp<MedicalBountyComponent>(target, out var bounty))
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-price-gun-pricing-result", ("object", Identity.Entity(target, EntityManager)), ("price", BankSystemExtensions.ToSpesoString(bounty.MaxBountyValue))), user, user);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-price-gun-pricing-result-none", ("object", Identity.Entity(target, EntityManager))), user, user);
        }
    }
}

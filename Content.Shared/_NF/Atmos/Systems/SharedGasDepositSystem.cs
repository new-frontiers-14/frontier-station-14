using Content.Shared._NF.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Shared._NF.Atmos.Systems;

public abstract class SharedGasDepositSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<GasDepositExtractorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<GasDepositExtractorComponent, ActivateInWorldEvent>(OnPumpActivate);
    }

    private void OnExamined(Entity<GasDepositExtractorComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined",
            ("statusColor", "lightblue"),
            ("pressure", ent.Comp.TargetPressure)));
        if (TryComp(ent.Comp.DepositEntity, out GasDepositComponent? deposit))
        {
            args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined-amount",
                ("statusColor", "lightblue"),
                ("value", deposit.Deposit.TotalMoles)));
        }
    }

    public void OnAnchorAttempt(Entity<GasDepositExtractorComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent, out TransformComponent? xform)
            || xform.GridUid is not { Valid: true } grid
            || !TryComp(grid, out MapGridComponent? gridComp))
        {
            args.Cancel();
            return;
        }

        var indices = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        while (enumerator.MoveNext(out var otherEnt))
        {
            // Don't match yourself.
            if (otherEnt == ent)
                continue;

            // Is another storage entity is already anchored here?
            if (!HasComp<GasDepositComponent>(otherEnt))
                continue;

            ent.Comp.DepositEntity = otherEnt.Value;
            return;
        }

        _popup.PopupPredicted(Loc.GetString("gas-deposit-drill-no-resources"), ent, args.User);
        args.Cancel();
    }

    private void OnPumpActivate(Entity<GasDepositExtractorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        if (Transform(ent).Anchored)
        {
            UI.OpenUi(ent.Owner, GasPressurePumpUiKey.Key, actor.PlayerSession);
            Dirty(ent);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("ui-needs-anchor"), args.User);
        }

        args.Handled = true;
    }
}

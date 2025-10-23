using Content.Shared._NF.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._NF.Atmos.Systems;

public abstract class SharedGasDepositSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    // The amount reported in a given extractor is a multiple of this.
    const float DrillExamineAmountRound = 1000.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
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

        if (_net.IsServer && TryComp(ent.Comp.DepositEntity, out GasDepositComponent? deposit))
        {
            float estimatedAmount = MathF.Round(deposit.Deposit.TotalMoles / DrillExamineAmountRound) * DrillExamineAmountRound;
            args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined-amount",
                ("statusColor", "lightblue"),
                ("value", estimatedAmount)));
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
            // Look for gas deposits, don't match yourself.
            if (otherEnt == ent || !HasComp<GasDepositComponent>(otherEnt))
                continue;

            ent.Comp.DepositEntity = otherEnt.Value;
            return;
        }
    }

    public void OnAnchorChanged(Entity<GasDepositExtractorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            ent.Comp.DepositEntity = null;
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

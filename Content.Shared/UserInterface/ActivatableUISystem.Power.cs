using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Robust.Shared.Containers;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIOpenedEvent>(OnBatteryOpened);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIClosedEvent>(OnBatteryClosed);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, ActivatableUIOpenAttemptEvent>(OnBatteryOpenAttempt);
    }

    private void OnToggled(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref ItemToggledEvent args)
    {
        // only close ui when losing power
        if (args.Activated || !TryComp<ActivatableUIComponent>(ent, out var activatable))
            return;

        if (activatable.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(ent)}");
            return;
        }

        _uiSystem.CloseUi(ent.Owner, activatable.Key);
    }

    private void OnBatteryOpened(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIOpenedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(uid);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        _toggle.TryActivate(uid);
    }

    private void OnBatteryClosed(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIClosedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(uid);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        // Stop drawing power if this was the last person with the UI open.
        if (!_uiSystem.IsUiOpen(uid, activatable.Key))
            _toggle.TryDeactivate(uid);
    }

    /// <summary>
    /// Call if you want to check if the UI should close due to a recent battery usage.
    /// </summary>
    public void CheckUsage(EntityUid uid, ActivatableUIComponent? active = null, ActivatableUIRequiresPowerCellComponent? component = null, PowerCellDrawComponent? draw = null)
    {
        if (!Resolve(uid, ref component, ref draw, ref active, false))
            return;

        if (active.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(uid)}");
            return;
        }

        if (_cell.HasActivatableCharge(uid))
            return;

        _uiSystem.CloseUi(uid, active.Key);
    }

    private void OnBatteryOpenAttempt(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if we have the appropriate drawrate / userate to even open it.
        // Don't pass in the user for the popup if silent.
        if (!_cell.HasActivatableCharge(uid, user: args.Silent ? null : args.User) ||
            !_cell.HasDrawCharge(uid, user: args.Silent ? null : args.User))
        {
            args.Cancel();
        }
    }
}

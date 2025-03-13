using Content.Shared._NF.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Visuals;
using Content.Shared._NF.DeviceLinking.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Examine;
using static Content.Shared._NF.DeviceLinking.Visuals.RngDeviceVisuals;
using Content.Shared.UserInterface;
using Content.Shared._NF.DeviceLinking.Systems;

namespace Content.Client._NF.DeviceLinking.Systems;

// Client-side system for RNG device functionality
public sealed class RngDeviceSystem : SharedRngDeviceSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private Dictionary<EntityUid, RngDeviceBoundUserInterfaceState> _lastStates = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RngDeviceVisualsComponent, RollEvent>(OnRoll);
        SubscribeLocalEvent<RngDeviceVisualsComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RngDeviceVisualsComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnUIOpened(Entity<RngDeviceVisualsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not RngDeviceUiKey.Key)
            return;

        // Store the UI state for later use in examine
        if (_ui.TryGetUiState<RngDeviceBoundUserInterfaceState>(args.Entity, RngDeviceUiKey.Key, out var state) &&
            state is RngDeviceBoundUserInterfaceState rngState)
        {
            _lastStates[args.Entity] = rngState;
        }
    }

    private void OnExamine(Entity<RngDeviceVisualsComponent> ent, ref ExaminedEvent args)
    {
        // Try to get the last UI state for this entity
        if (!_lastStates.TryGetValue(ent, out var state))
            return;

        // Use args.PushGroup to organize the examine text
        using (args.PushGroup("RngDevice"))
        {
            args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", state.LastRoll)));

            if (state.Outputs == 2)  // Only show port info for percentile die
                args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", state.LastOutputPort)));
        }
    }

    private void OnRoll(Entity<RngDeviceVisualsComponent> ent, ref RollEvent args)
    {
        if (args.Handled)
            return;

        PredictRoll(ent, args.Outputs, args.User);
        args.Handled = true;
    }

    // Predicts a roll on the client side for responsive UI
    private void PredictRoll(Entity<RngDeviceVisualsComponent> ent, int outputs, EntityUid? user = null)
    {
        // Use the shared GenerateRoll method
        var (roll, _) = GenerateRoll(outputs);

        // Update visuals with the predicted roll
        UpdateVisualState(ent, outputs, roll);

        // Show popup
        var popupString = Loc.GetString("rng-device-rolled", ("value", roll));
        _popup.PopupPredicted(popupString, ent, user);
    }

    private void UpdateVisualState(Entity<RngDeviceVisualsComponent> ent, int outputs, int roll)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var statePrefix = outputs == 2 ? "percentile" : $"d{outputs}";
        var stateNumber = outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(ent, State, $"{statePrefix}_{stateNumber}", appearance);
    }
}

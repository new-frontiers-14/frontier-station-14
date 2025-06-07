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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RngDeviceComponent, AfterAutoHandleStateEvent>(OnRngDeviceState);
        SubscribeLocalEvent<RngDeviceVisualsComponent, RollEvent>(OnRoll);
        SubscribeLocalEvent<RngDeviceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnRngDeviceState(Entity<RngDeviceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Update any open BUIs when component data changes
        if (_ui.TryGetOpenUi(ent.Owner, RngDeviceUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnExamine(Entity<RngDeviceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Use args.PushGroup to organize the examine text
        using (args.PushGroup("RngDevice"))
        {
            args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", ent.Comp.LastRoll)));

            if (ent.Comp.Outputs == 2)  // Only show port info for percentile die
                args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", ent.Comp.LastOutputPort)));
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
        int roll;
        // Only use target number for percentile dice (outputs == 2)
        if (outputs == 2 && TryComp<RngDeviceComponent>(ent, out var rngComp))
        {
            // Use the overload that takes targetNumber
            (roll, _) = GenerateRoll(outputs, rngComp.TargetNumber);
        }
        else
        {
            // Use the original overload without targetNumber
            (roll, _) = GenerateRoll(outputs);
        }

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

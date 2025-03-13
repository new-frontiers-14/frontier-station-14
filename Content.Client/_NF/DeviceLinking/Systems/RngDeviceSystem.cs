using Content.Shared._NF.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Visuals;
using Content.Shared._NF.DeviceLinking.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using static Content.Shared._NF.DeviceLinking.Visuals.RngDeviceVisuals;

namespace Content.Client._NF.DeviceLinking.Systems;

/// <summary>
/// Client-side system for RNG device functionality
/// </summary>
public sealed class RngDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RngDeviceVisualsComponent, RollEvent>(OnRoll);
    }

    private void OnRoll(Entity<RngDeviceVisualsComponent> ent, ref RollEvent args)
    {
        if (args.Handled)
            return;

        PredictRoll(ent, args.Outputs, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Predicts a roll on the client side for responsive UI
    /// </summary>
    private void PredictRoll(Entity<RngDeviceVisualsComponent> ent, int outputs, EntityUid? user = null)
    {
        // Use current tick as seed for deterministic randomness
        var rand = new System.Random((int)_timing.CurTick.Value);

        int roll;

        if (outputs == 2)
        {
            // For percentile dice, roll 1-100
            roll = rand.Next(1, 101);
        }
        else
        {
            roll = rand.Next(1, outputs + 1);
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

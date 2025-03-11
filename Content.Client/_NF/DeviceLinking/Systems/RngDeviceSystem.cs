using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Systems;
using Robust.Client.GameObjects;
using static Content.Shared._NF.DeviceLinking.RngDeviceVisuals;

namespace Content.Client._NF.DeviceLinking.Systems;

// Client-side system for RNG device functionality
public sealed class RngDeviceSystem : SharedRngDeviceSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RngDeviceComponent> ent, ref ComponentInit args)
    {
        // Initialize the state prefix
        ent.Comp.StatePrefix = GetStatePrefix(ent, ent.Comp);
    }

    // Predicts a roll on the client side for responsive UI
    public void PredictRoll(Entity<RngDeviceComponent> ent)
    {
        // Use the shared PerformRoll method which uses the current tick for deterministic randomness
        var (roll, _) = PerformRoll(ent);

        // Update visuals with the predicted roll
        UpdateVisualState(ent, roll);
    }

    private void UpdateVisualState(Entity<RngDeviceComponent> ent, int roll)
    {
        var comp = ent.Comp;

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var stateNumber = comp.Outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(ent, State, $"{comp.StatePrefix}_{stateNumber}", appearance);
    }

    protected override void UpdateVisuals(Entity<RngDeviceComponent> ent)
    {
        UpdateVisualState(ent, ent.Comp.LastRoll);
    }
}

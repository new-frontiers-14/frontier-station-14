using Content.Shared._NF.Trade;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Trade;

/// <summary>
///     Visualizer for trade crates, largely based on Nyano's mail visualizer (thank you)
/// </summary>
public sealed class TradeCrateVisualizerSystem : VisualizerSystem<TradeCrateComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private const string FallbackIconID = "CargoOther";
    private const string CargoPriorityActiveState = "cargo_priority";
    private const string CargoPriorityInactiveState = "cargo_priority_inactive";

    protected override void OnAppearanceChange(EntityUid uid, TradeCrateComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData(uid, TradeCrateVisuals.DestinationIcon, out string job, args.Component);

        if (string.IsNullOrEmpty(job))
            job = FallbackIconID;

        if (!_prototypeManager.TryIndex<TradeCrateDestinationPrototype>(job, out var icon))
            icon = _prototypeManager.Index<TradeCrateDestinationPrototype>(FallbackIconID);

        args.Sprite.LayerSetTexture(TradeCrateVisualLayers.Icon, _spriteSystem.Frame0(icon.Icon));
        if (_appearance.TryGetData(uid, TradeCrateVisuals.IsPriority, out bool isPriority) && isPriority)
        {
            args.Sprite.LayerSetVisible(TradeCrateVisualLayers.Priority, true);
            if (_appearance.TryGetData(uid, TradeCrateVisuals.IsPriorityInactive, out bool inactive) && inactive)
                args.Sprite.LayerSetState(TradeCrateVisualLayers.Priority, CargoPriorityInactiveState);
            else
                args.Sprite.LayerSetState(TradeCrateVisualLayers.Priority, CargoPriorityActiveState);
        }
        else
        {
            args.Sprite.LayerSetVisible(TradeCrateVisualLayers.Priority, false);
        }
    }
}

public enum TradeCrateVisualLayers : byte
{
    Icon,
    Priority
}

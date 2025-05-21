using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Research.Prototypes;
using Content.Client.Lathe;
using Content.Shared._NF.Lathe;
using Robust.Shared.Prototypes;
using Content.Shared._NF.Research.Prototypes;

namespace Content.Client._NF.Lathe;

// Effectively a carbon copy of the existing lathe system.
public sealed class BlueprintLatheSystem : SharedBlueprintLatheSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlueprintLatheComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, BlueprintLatheComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Lathe specific stuff
        if (_appearance.TryGetData<bool>(uid, LatheVisuals.IsRunning, out var isRunning, args.Component))
        {
            if (args.Sprite.LayerMapTryGet(LatheVisualLayers.IsRunning, out var runningLayer) &&
                component.RunningState != null &&
                component.IdleState != null)
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                args.Sprite.LayerSetState(runningLayer, state);
            }
        }

        if (_appearance.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var powered, args.Component) &&
            args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out var powerLayer))
        {
            args.Sprite.LayerSetVisible(powerLayer, powered);

            if (component.UnlitIdleState != null &&
                component.UnlitRunningState != null)
            {
                var state = isRunning ? component.UnlitRunningState : component.UnlitIdleState;
                args.Sprite.LayerSetState(powerLayer, state);
            }
        }
    }

    ///<remarks>
    /// Whether or not a set of recipes is available is not really visible to the client,
    /// so this defaults to true.
    ///</remarks>
    protected override bool HasRecipes(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, int[] recipes, BlueprintLatheComponent component)
    {
        return true;
    }

    ///<remarks>
    /// Whether or not a recipe is available is not really visible to the client,
    /// so this defaults to true.
    ///</remarks>
    protected override bool HasRecipe(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, ProtoId<LatheRecipePrototype> recipe, BlueprintLatheComponent component)
    {
        return true;
    }
}


using Content.Server._NF.River.Components;
using Content.Server._NF.FlyableGridPopulator.Components;

namespace Content.Server._NF.FlyableGridPopulator;

/// <summary>
/// This system populates and depopulates ships/shuttles with components necessary for their functioning.
/// It does this when an entity with a FlyableGridPopulator component anchors with a grid, or the last FlyableGridPopulator unanchors.
/// </summary>
public sealed partial class FlyableGridPopulatorSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlyableGridPopulatorComponent, AnchorStateChangedEvent>(OnAnchorChange);
    }

    private void OnAnchorChange(EntityUid uid, FlyableGridPopulatorComponent component, AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            if (args.Transform.GridUid != null)
            {
                var gridUid = (EntityUid)args.Transform.GridUid;
                PopulateShuttleComponents(gridUid);
                component.ShuttleGrid = gridUid;
            }
        }
        else
        {
            if (component.ShuttleGrid != null)
            {
                var gridUid = (EntityUid)component.ShuttleGrid;
                if (DepopulateShuttleComponents(gridUid))
                {
                    component.ShuttleGrid = null;
                }
            }
        }
    }

    /// <summary>
    /// Populates the anchored to grid with the relevant components.
    /// </summary>
    /// <param name="gridUid">The grid's EntityUid</param>
    private void PopulateShuttleComponents(EntityUid gridUid)
    {
        //vvv Add any components you wish to add to a Ship/Shuttle below here vvv
        EnsureComp<RiverFlowReceiverComponent>(gridUid);

        //^^^ Add any components you wish to add to a Ship/Shuttle above here ^^^
    }

    /// <summary>
    /// Removes the relevant components from the unanchored from grid if this component is the last FlyableGridPopulator to unanchor.
    /// </summary>
    /// <param name="gridUid">The grid's EntityUid</param>
    private bool DepopulateShuttleComponents(EntityUid gridUid)
    {
        var depopulated = true;
        HashSet<Entity<FlyableGridPopulatorComponent>> populators = new();
        _lookup.GetGridEntities<FlyableGridPopulatorComponent>(gridUid, populators);
        foreach (var populator in populators)
        {
            if (Transform(populator).Anchored)
            {
                depopulated = false;
                break;
            }
        }

        if (depopulated)
        {
            //vvv Add any components you wish to remove to a Ship/Shuttle below here vvv
            RemCompDeferred<RiverFlowReceiverComponent>(gridUid);

            //^^^ Add any components you wish to remove to a Ship/Shuttle Above here ^^^
        }
        return depopulated;
    }
}

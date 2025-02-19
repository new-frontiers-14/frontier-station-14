using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Chemistry.Systems;

public sealed class ReplaceEntityOnSolutionEmptySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplaceEntityOnSolutionEmptyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplaceEntityOnSolutionEmptyComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    public void OnMapInit(Entity<ReplaceEntityOnSolutionEmptyComponent> entity, ref MapInitEvent args)
    {
        CheckSolutions(entity);
    }

    public void OnSolutionChange(Entity<ReplaceEntityOnSolutionEmptyComponent> entity, ref SolutionContainerChangedEvent args)
    {
        CheckSolutions(entity);
    }

    public void CheckSolutions(Entity<ReplaceEntityOnSolutionEmptyComponent> entity)
    {
        if (!EntityManager.HasComponent<SolutionContainerManagerComponent>(entity))
            return;

        if (_solution.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution) && solution.Volume <= 0)
            ReplaceEntity(entity);
    }

    public void ReplaceEntity(Entity<ReplaceEntityOnSolutionEmptyComponent> entity)
    {
        var position = _transform.GetMapCoordinates(entity);
        var inContainer = _container.TryGetContainingContainer((entity, null), out var container);
        var replacementEntity = entity.Comp.ReplacementEntity;

        Del(entity);

        if (inContainer && container != null)
            SpawnInContainerOrDrop(replacementEntity, container.Owner, container.ID);
        else
            Spawn(replacementEntity, position);
    }
}

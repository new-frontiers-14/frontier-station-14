using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;


namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class SolutionInjectOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolutionInjectOnCollideComponent, StartCollideEvent>(HandleInjection);
        }

        private void HandleInjection(EntityUid uid, SolutionInjectOnCollideComponent component, ref StartCollideEvent args)
        {
            var target = args.OtherEntity;

            if (!args.OtherBody.Hard ||
                !EntityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream) ||
                !_solutionsSystem.TryGetInjectableSolution(component.Owner, out var solution)) return;

            if (component.BlockSlots != 0x0 && TryComp<InventoryComponent>(target, out var inventory))
            {
                var containerEnumerator = new InventorySystem.ContainerSlotEnumerator(target, inventory.TemplateId, _protoManager, _inventorySystem, component.BlockSlots);

                while (containerEnumerator.MoveNext(out var container))
                {
                    if (!container.ContainedEntity.HasValue) continue;
                    return;
                }
            }

            var solRemoved = solution.SplitSolution(component.TransferAmount);
            var solRemovedVol = solRemoved.Volume;

            var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);

            _bloodstreamSystem.TryAddToChemicals(target, solToInject, bloodstream);
        }
    }
}

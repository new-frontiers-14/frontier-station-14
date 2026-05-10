using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

/* Mostly duplicated code from "Content/Shared/Construction/Steps/MaterialConstructionGraphStep.cs" but checking
 * for machine part type instead of material.
 * Done so to left upstream code mostly untouched for easier future maintenance.
 */

namespace Content.Shared.Construction.Steps // NOTE: currently exists under base namespace.
{
    [DataDefinition]
    public sealed partial class MachinePartConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("part", required:true)]
        public ProtoId<MachinePartPrototype> PartPrototypeId { get; private set; }
        [DataField] public int Amount { get; private set; } = 1;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            if (!entityManager.TryGetComponent(uid, out StackComponent? stack) || stack.Count < Amount)
                return false;

            return entityManager.TryGetComponent(uid, out MachinePartComponent? part) && part.PartType == PartPrototypeId;
        }

        public bool EntityValid(EntityUid entity, [NotNullWhen(true)] out StackComponent? stack)
        {
            stack = null;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out MachinePartComponent? part) && part.PartType != PartPrototypeId
                && IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out StackComponent? otherStack) && otherStack.Count >= Amount)
            {
                stack = otherStack;
            }

            return stack != null;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var part = IoCManager.Resolve<IPrototypeManager>().Index(PartPrototypeId);
            var partName = Loc.GetString(part.ID, ("amount", Amount));

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", partName)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var part = IoCManager.Resolve<IPrototypeManager>().Index(PartPrototypeId);
            var partName = Loc.GetString(part.Name, ("amount", Amount));

            return new ConstructionGuideEntry()
            {
                Localization = "construction-presenter-machine-part-step",
                Arguments = new (string, object)[]{("amount", Amount), ("part", partName)},
                Icon = part.Icon
            };
        }
    }
}

using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class MachinePartConstructionGraphStep : StackInvolvingConstructionGraphStep
    {
        [DataField("part", required:true)]
        public ProtoId<MachinePartPrototype> PartPrototypeId { get; private set; }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var part = IoCManager.Resolve<IPrototypeManager>().Index(PartPrototypeId);
            var partName = Loc.GetString(part.ID, ("amount", Amount));

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", partName)));
        }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            if(!entityManager.TryGetComponent(uid, out MachinePartComponent? part) || part.PartType != PartPrototypeId)
                return false;
            return entityManager.TryGetComponent(uid, out StackComponent? stack) && stack.Count >= Amount;
        }

        public override bool EntityValid(EntityUid uid, [NotNullWhen(true)] out StackComponent? stack)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(uid, out MachinePartComponent? part) || part.PartType != PartPrototypeId) {
                stack = null;
                return false;
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(uid, out StackComponent? otherStack) && otherStack.Count >= Amount)
                stack = otherStack;
            else
                stack = null;

            return stack != null;
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

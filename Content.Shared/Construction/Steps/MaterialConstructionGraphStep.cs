using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class MaterialConstructionGraphStep : StackInvolvingConstructionGraphStep //Frontier: EntityInsertConstructionGraphStep<StackInvolvingConstructionGraphStep
    {
        // TODO: Make this use the material system.
        // TODO TODO: Make the material system not shit.
        [DataField("material", required:true)]
        public ProtoId<StackPrototype> MaterialPrototypeId { get; private set; }

        //Frontier: moved to the abstract StackInvolvingConstructionGraphStep class
        //[DataField] public int Amount { get; private set; } = 1;

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var material = IoCManager.Resolve<IPrototypeManager>().Index(MaterialPrototypeId);
            var materialName = Loc.GetString(material.Name, ("amount", Amount));

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", materialName)));
        }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            if (!entityManager.TryGetComponent(uid, out StackComponent? stack) || stack.StackTypeId != MaterialPrototypeId)
                return false;

            return base.EntityValid(uid, entityManager, compFactory);
        }

        //Frontier
        public override bool EntityValid(EntityUid entity, [NotNullWhen(true)] out StackComponent? stack) //Frontier: added override
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out StackComponent? otherStack) || otherStack.StackTypeId != MaterialPrototypeId)
            {
                stack = null;
                return false;
            }

            return base.EntityValid(entity, out stack);
        }
        //End Frontier

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var material = IoCManager.Resolve<IPrototypeManager>().Index(MaterialPrototypeId);
            var materialName = Loc.GetString(material.Name, ("amount", Amount));

            return new ConstructionGuideEntry()
            {
                Localization = "construction-presenter-material-step",
                Arguments = new (string, object)[]{("amount", Amount), ("material", materialName)},
                Icon = material.Icon,
            };
        }
    }
}

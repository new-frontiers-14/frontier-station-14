using Content.Shared.Stacks;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Construction.Steps // NOTE: currently exists under base namespace.
{
    public abstract partial class StackInvolvingConstructionGraphStep : EntityInsertConstructionGraphStep
    {

        [DataField] public int Amount { get; private set; } = 1;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            return entityManager.TryGetComponent(uid, out StackComponent? stack) && stack.Count >= Amount;
        }

        public virtual bool EntityValid(EntityUid entity, [NotNullWhen(true)] out StackComponent? stack)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out StackComponent? otherStack) && otherStack.Count >= Amount)
                stack = otherStack;
            else
                stack = null;

            return stack != null;
        }
    }
}

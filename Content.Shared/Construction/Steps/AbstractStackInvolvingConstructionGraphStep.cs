using System.Diagnostics.CodeAnalysis;
using Content.Shared.Stacks;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public abstract partial class StackInvolvingConstructionGraphStep : EntityInsertConstructionGraphStep
    {

        [DataField] public int Amount { get; private set; } = 1;

        public abstract bool EntityValid(EntityUid entity, [NotNullWhen(true)] out StackComponent? stack);
    }
}

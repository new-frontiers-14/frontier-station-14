using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class DieCondition : IObjectiveCondition
    {
        private MindComponent? _mind;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            return new DieCondition { _mind = mind };
        }

        public string Title => Loc.GetString("objective-condition-die-title");

        public string Description => Loc.GetString("objective-condition-die-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Mobs/Ghosts/ghost_human.rsi"), "icon");

        public float Progress
        {
            get
            {
                var entityManager = IoCManager.Resolve<EntityManager>();
                var mindSystem = entityManager.System<SharedMindSystem>();
                return _mind == null || mindSystem.IsCharacterDeadIc(_mind) ? 1f : 0f;
            }
        }

        public float Difficulty => 0.5f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is DieCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DieCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}

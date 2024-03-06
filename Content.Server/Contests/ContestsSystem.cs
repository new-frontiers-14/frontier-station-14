using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Components;

// Frontier: this class has been removed in upstream pPR #25425;
// the removal was reverted to preserve the old behaviors of various systems.
namespace Content.Server.Contests
{
    /// <summary>
    /// Standardized contests.
    /// A contest is figuring out, based on data in components on two entities,
    /// which one has an advantage in a situation. The advantage is expressed by a multiplier.
    /// 1 = No advantage to either party.
    /// &gt;1 = Advantage to roller
    /// &lt;1 = Advantage to target
    /// Roller should be the entity with an advantage from being bigger/healthier/more skilled, etc.
    /// </summary>
    ///
    public sealed class ContestsSystem : EntitySystem
    {
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        /// <summary>
        /// Returns the roller's mass divided by the target's.
        /// </summary>
        public float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
        {
            if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
                return 1f;

            if (targetPhysics.FixturesMass == 0)
                return 1f;

            return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
        }
    }
}

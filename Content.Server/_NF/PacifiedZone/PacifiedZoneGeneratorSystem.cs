using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;


namespace Content.Server._NF.PacifiedZone
{
    public sealed class PacifiedZoneGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedJobSystem _jobSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacifiedZoneGeneratorComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PacifiedZoneGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
        }

        private void OnComponentInit(EntityUid uid, PacifiedZoneGeneratorComponent component, ComponentInit args)
        {
            foreach (var humanoid_uid in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, component.Radius))
            {
                if (HasComp<PacifiedComponent>(humanoid_uid))
                    continue;

                if (!_mindSystem.TryGetMind(humanoid_uid, out var mindId, out var _))
                    continue;

                _jobSystem.MindTryGetJobId(mindId, out var jobId);

                if (jobId != null && component.ImmuneRoles.Contains(jobId.Value))
                    continue;

                AddComp<PacifiedComponent>(humanoid_uid);
                AddComp<PacifiedByZoneComponent>(humanoid_uid);
                component.TrackedEntities.Add(humanoid_uid);
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
        }

        private void OnComponentShutdown(EntityUid uid, PacifiedZoneGeneratorComponent component, ComponentShutdown args)
        {
            foreach (var entity in component.TrackedEntities)
            {
                RemComp<PacifiedComponent>(entity);
                RemComp<PacifiedByZoneComponent>(entity);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var gen_query = AllEntityQuery<PacifiedZoneGeneratorComponent>();
            while (gen_query.MoveNext(out var gen_uid, out var component))
            {
                List<EntityUid> newEntities = new List<EntityUid>();
                // Not yet update time, skip this 
                if (_gameTiming.CurTime < component.NextUpdate)
                    continue;

                var query = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(gen_uid).Coordinates, component.Radius);
                foreach (var humanoid_uid in query)
                {
                    if (!_mindSystem.TryGetMind(humanoid_uid, out var mindId, out var mind))
                        continue;

                    _jobSystem.MindTryGetJobId(mindId, out var jobId);

                    // Player matches an immune role, should not be pacified.
                    if (jobId != null && component.ImmuneRoles.Contains(jobId.Value))
                        continue;

                    if (component.TrackedEntities.Contains(humanoid_uid))
                    {
                        // Entity still in zone.
                        newEntities.Add(humanoid_uid);
                        component.TrackedEntities.Remove(humanoid_uid);
                    }
                    else
                    {
                        // Player is pacified (either naturally or by another zone), skip them.
                        if (HasComp<PacifiedComponent>(humanoid_uid))
                            continue;

                        // New entity in zone, needs the Pacified comp.
                        AddComp<PacifiedComponent>(humanoid_uid);
                        AddComp<PacifiedByZoneComponent>(humanoid_uid);
                        newEntities.Add(humanoid_uid);
                    }
                }

                // Anything left in our old set has left the zone, remove their pacified status.
                foreach (var humanoid_net_uid in component.TrackedEntities)
                {
                    RemComp<PacifiedComponent>(humanoid_net_uid);
                    RemComp<PacifiedByZoneComponent>(humanoid_net_uid);
                }

                // Update state for next run.
                component.TrackedEntities = newEntities;
                component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
            }
        }
    }
}
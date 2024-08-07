using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;


namespace Content.Server._NF.PacifiedZone
{
    public sealed class PacifiedZoneGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedJobSystem _jobSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacifiedZoneGeneratorComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, PacifiedZoneGeneratorComponent component, ComponentInit args)
        {
            foreach (var humanoid_uid in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, component.Radius))
            {
                if (TryComp<PacifiedComponent>(humanoid_uid, out var pacifComp))
                    continue;

                if (!_mindSystem.TryGetMind(humanoid_uid, out var mindId, out var mind))
                    continue;

                _jobSystem.MindTryGetJobId(mindId, out var jobId);

                if (jobId != null)
                    if (component.RolesImmun.Contains(jobId!.Value))
                        continue;

                AddComp<PacifiedComponent>(humanoid_uid);
                component.OldListEntities.Add(_entMan.GetNetEntity(humanoid_uid));
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var gen_query = AllEntityQuery<PacifiedZoneGeneratorComponent>();
            while (gen_query.MoveNext(out var gen_uid, out var component))
            {
                var query = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(gen_uid).Coordinates, component.Radius);
                foreach (var humanoid_uid in query)
                {
                    if (!_mindSystem.TryGetMind(humanoid_uid, out var mindId, out var mind))
                        continue;

                    _jobSystem.MindTryGetJobId(mindId, out var jobId);

                    if (jobId != null)
                        if (component.RolesImmun.Contains(jobId!.Value))
                            continue;

                    if (component.OldListEntities.Contains(_entMan.GetNetEntity(humanoid_uid)))
                    {
                        component.IntermediateListEntities.Add(_entMan.GetNetEntity(humanoid_uid));
                        component.OldListEntities.Remove(_entMan.GetNetEntity(humanoid_uid));
                    }
                    else
                    {
                        AddComp<PacifiedComponent>(humanoid_uid);
                        component.IntermediateListEntities.Add(_entMan.GetNetEntity(humanoid_uid));
                    }
                }

                foreach (var humanoid_net_uid in component.OldListEntities)
                {
                    RemComp<PacifiedComponent>(GetEntity(humanoid_net_uid));
                }

                component.OldListEntities.Clear();
                component.OldListEntities.AddRange(component.IntermediateListEntities);
                component.IntermediateListEntities.Clear();

                component.NextUpdate += component.UpdateInterval;
            }
        }
    }
}
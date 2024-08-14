using Robust.Server.Player;
using Robust.Shared.Timing;
using Content.Server.Administration.Systems;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Server.Players.PlayTimeTracking;

namespace Content.Server._NF.PacifiedZone
{
    public sealed class PacifiedZoneGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedJobSystem _jobSystem = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly PlayTimeTrackingManager _playtimeTracking = default!;
        [Dependency] private readonly AdminSystem _admin = default!;

        private const string Alert = "PacifiedZone";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacifiedZoneGeneratorComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PacifiedZoneGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
        }

        private void OnComponentInit(EntityUid uid, PacifiedZoneGeneratorComponent component, ComponentInit args)
        {
            UpdatePacifiedState(uid, component);
        }

        private void OnComponentShutdown(EntityUid uid, PacifiedZoneGeneratorComponent component, ComponentShutdown args)
        {
            foreach (var entity in component.TrackedEntities)
            {
                RemComp<PacifiedComponent>(entity);
                RemComp<PacifiedByZoneComponent>(entity);
                DisableAlert(entity);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var genQuery = AllEntityQuery<PacifiedZoneGeneratorComponent>();
            while (genQuery.MoveNext(out var genUid, out var component))
            {
                // Not yet update time, skip this 
                if (_gameTiming.CurTime < component.NextUpdate)
                    continue;

                UpdatePacifiedState(genUid, component);
            }
        }

        private void UpdatePacifiedState(EntityUid genUid, PacifiedZoneGeneratorComponent component)
        {
            List<EntityUid> newEntities = new List<EntityUid>();
            var query = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(genUid).Coordinates, component.Radius);
            foreach (var humanoidUid in query)
            {
                // Check preconditions for an entity to be pacified at all.
                // If player matches an immune role, or has playtime above a zone's threshold, it should not be pacified.
                if (!_mindSystem.TryGetMind(humanoidUid, out var mindId, out var mind))
                    continue;

                _jobSystem.MindTryGetJobId(mindId, out var jobId);

                if (jobId != null && component.ImmuneRoles.Contains(jobId.Value))
                    continue;

                if (component.ImmunePlaytime != null)
                {
                    var playerInfo = _admin.GetCachedPlayerInfo(mind?.UserId);
                    if (playerInfo != null && playerInfo.OverallPlaytime >= component.ImmunePlaytime)
                    {
                        continue;
                    }
                }

                // Existing entity, note it still exists.
                if (component.TrackedEntities.Contains(humanoidUid))
                {
                    // Entity still in zone.
                    newEntities.Add(humanoidUid);
                    component.TrackedEntities.Remove(humanoidUid);
                }
                else
                {
                    // Player is pacified (either naturally or by another zone), skip them.
                    if (HasComp<PacifiedComponent>(humanoidUid))
                        continue;

                    // New entity in zone, needs the Pacified comp.
                    var pacifiedComponent = AddComp<PacifiedComponent>(humanoidUid);
                    EnableAlert(humanoidUid, pacifiedComponent);
                    AddComp<PacifiedByZoneComponent>(humanoidUid);
                    newEntities.Add(humanoidUid);
                }
            }

            // Anything left in our old set has left the zone, remove their pacified status.
            foreach (var humanoid_net_uid in component.TrackedEntities)
            {
                RemComp<PacifiedComponent>(humanoid_net_uid);
                RemComp<PacifiedByZoneComponent>(humanoid_net_uid);
                DisableAlert(humanoid_net_uid);
            }
            // Update state for next run.
            component.TrackedEntities = newEntities;
            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
        }

        // Overrides the default Pacified alert with one for the pacified zone.
        private void EnableAlert(EntityUid entity, PacifiedComponent pacified)
        {
            _alerts.ClearAlert(entity, pacified.PacifiedAlert);
            _alerts.ShowAlert(entity, Alert);
        }

        // Hides our pacified zone alert.
        private void DisableAlert(EntityUid entity)
        {
            _alerts.ClearAlert(entity, Alert);
        }
    }
}
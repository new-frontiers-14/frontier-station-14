using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Content.Shared.Inventory;
using Robust.Server.Player;
using Content.Shared._NF.AirFreshener.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles stink, causing the affected to stink uncontrollably at a random interval.
/// </summary>
public sealed class StinkyTraitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StinkyTraitComponent, ComponentStartup>(SetupStinkyTrait);
        SubscribeLocalEvent<StinkyTraitComponent, ExaminedEvent>(OnExamined);
    }

    private void SetupStinkyTrait(EntityUid uid, StinkyTraitComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public void AdjustStinkyTraitTimer(EntityUid uid, int TimerReset, StinkyTraitComponent? stinky = null)
    {
        if (!Resolve(uid, ref stinky, false))
            return;

        stinky.NextIncidentTime = TimerReset;
    }

    private void OnExamined(EntityUid uid, StinkyTraitComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient && component.IsActive)
        {
            args.PushMarkup(Loc.GetString("trait-stinky-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StinkyTraitComponent>();
        while (query.MoveNext(out var uid, out var stinky))
        {
            /// <summary>
            /// Finds the entity that can hold an uplink for a user.
            /// Usually this is a pda in their pda slot, but can also be in their hands. (but not pockets or inside bag, etc.)
            /// </summary>
            ///

            //if (_inventory.TryGetContainerSlotEnumerator(uid, out var containerSlotEnumerator))
            //{
            //    while (containerSlotEnumerator.MoveNext(out var item))
            //    {
            //        if (!item.ContainedEntity.HasValue)
            //            continue;

            //        if (HasComp<AirFreshenerComponent>(item.ContainedEntity.Value))
            //            stinky.IsActive = false;
            //        else
            //            stinky.IsActive = true;
            //    }
            //}

            stinky.NextIncidentTime -= frameTime;

            if (stinky.NextIncidentTime >= 0)
                continue;

            if (_inventory.TryGetSlotEntity(uid, "pocket1", out var pocket1))
            {
                if (HasComp<AirFreshenerComponent>(pocket1))
                {
                    stinky.IsActive = false;
                    continue;
                }
            }
            else if (_inventory.TryGetSlotEntity(uid, "pocket2", out var pocket2))
            {
                if (HasComp<AirFreshenerComponent>(pocket2))
                {
                    stinky.IsActive = false;
                    continue;
                }
            }
            stinky.IsActive = true;

            // Set the new time.
            stinky.NextIncidentTime +=
                _random.NextFloat(stinky.TimeBetweenIncidents.X, stinky.TimeBetweenIncidents.Y);

            var duration = _random.NextFloat(stinky.DurationOfIncident.X, stinky.DurationOfIncident.Y);

            // Make sure the stink time doesn't cut into the time to next incident.
            stinky.NextIncidentTime += duration;

            if (!TryComp<TransformComponent>(uid, out var xform))
                continue;

            if (!TryComp<PhysicsComponent>(uid, out var physics))
                continue;

            var indices = _transform.GetGridOrMapTilePosition(uid);
            var tileMix = _atmosphere.GetTileMixture(xform.GridUid, null, indices, true);
            tileMix?.AdjustMoles(Gas.Miasma, 0.01f * physics.FixturesMass);
        }
    }
}

// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2024 Mervill <mervills.email@gmail.com>
// SPDX-FileCopyrightText: 2024 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 TinManTim <73014819+Tin-Man-Tim@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 corresp0nd <46357632+corresp0nd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Lightning;

// TheShuEd:
//I've redesigned the lightning system to be more optimized.
//Previously, each lightning element, when it touched something, would try to branch into nearby entities.
//So if a lightning bolt was 20 entities long, each one would check its surroundings and have a chance to create additional lightning...
//which could lead to recursive creation of more and more lightning bolts and checks.

//I redesigned so that lightning branches can only be created from the point where the lightning struck, no more collide checks
//and the number of these branches is explicitly controlled in the new function.
public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, LightningComponent component, ComponentRemove args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam) || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        beamController.CreatedBeams.Remove(uid);
    }

    /// <summary>
    /// Fires lightning from user to target
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="target">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    public void ShootLightning(EntityUid user, EntityUid target, string lightningPrototype = "Lightning", bool triggerLightningEvents = true)
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);

        if (triggerLightningEvents) // we don't want certain prototypes to trigger lightning level events
        {
            var ev = new HitByLightningEvent(user, target);
            RaiseLocalEvent(target, ref ev);
        }
    }

    /// <summary>
    /// Fires lightning from user to coordinates
    /// </summary>
    /// <remarks>
    ///     Ported from imp
    /// </remarks>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="targetCoordinates">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    public void ShootLightning(EntityUid user, MapCoordinates targetCoordinates, string lightningPrototype = "Lightning", bool triggerLightningEvents = true)
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, targetCoordinates, lightningPrototype, spriteState);
    }

    /// <summary>
    /// Fires lightning from coordinates to target
    /// </summary>
    /// <remarks>
    ///     Ported from imp
    /// </remarks>
    /// <param name="coordinates">Where the lightning fires from</param>
    /// <param name="target">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    public void ShootLightning(MapCoordinates coordinates, EntityUid target, string lightningPrototype = "Lightning", bool triggerLightningEvents = true)
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(coordinates, target, lightningPrototype, spriteState);

        if (triggerLightningEvents) // we don't want certain prototypes to trigger lightning level events
        {
            var ev = new HitByLightningEvent(null, target);
            RaiseLocalEvent(target, ref ev);
        }
    }

    /// <summary>
    /// Fires lightning from coordinates to other coordinates
    /// </summary>
    /// <remarks>
    ///     Ported from imp
    /// </remarks>
    /// <param name="coordinates">Where the lightning fires from</param>
    /// <param name="targetCoordinates">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    public void ShootLightning(MapCoordinates coordinates, MapCoordinates targetCoordinates, string lightningPrototype = "Lightning", bool triggerLightningEvents = true)
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(coordinates, targetCoordinates, lightningPrototype, spriteState);
    }


    /// <summary>
    /// Looks for objects with a LightningTarget component in the radius, prioritizes them, and hits the highest priority targets with lightning.
    /// </summary>
    /// <remarks>
    ///     Ported from imp.
    /// </remarks>
    /// <param name="coordinates">Where the lightning fires from</param>
    /// <param name="range">Targets selection radius</param>
    /// <param name="boltCount">Number of lightning bolts</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="arcDepth">how many times to recursively fire lightning bolts from the target points of the first shot.</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    /// <param name="hitCoordsChance">Chance for lightning to strike random coordinates instead of an entity.</param>
    /// <param name="user">The entity that is shooting lightning.</param>
    public void ShootRandomLightnings(
        MapCoordinates coordinates, // imp user -> coordinates
        float range,
        int boltCount,
        string lightningPrototype = "Lightning",
        int arcDepth = 0,
        bool triggerLightningEvents = true,
        float hitCoordsChance = 0f, // imp
        bool canExplode = true, // imp
        EntityUid? user = null) // imp
    {
        //TODO: add support to different priority target tablem for different lightning types
        //TODO: Remove Hardcode LightningTargetComponent (this should be a parameter of the SharedLightningComponent)
        //TODO: This is still pretty bad for perf but better than before and at least it doesn't re-allocate several hashsets every time

        var targets = _lookup.GetEntitiesInRange<LightningTargetComponent>(coordinates, range).ToList();
        _random.Shuffle(targets);
        targets.Sort((x, y) => y.Comp.Priority.CompareTo(x.Comp.Priority));

        int shootedCount = 0;
        int count = -1;
        int mobLightningResistance = 2; // imp - If the target has a lightning resistance less than or equal to this, the lightning can hit random coordinates instead.
        while (shootedCount < boltCount)
        {
            count++;

            // imp - random coordinate lightning start
            var outOfRange = count >= targets.Count;
            var targetLightningResistance = outOfRange ? 0 : targets[count].Comp.LightningResistance;

            if (hitCoordsChance > 0 && targetLightningResistance <= mobLightningResistance && _random.Prob(hitCoordsChance))
            {
                var targetCoordinate = coordinates.Offset(_random.NextVector2(range, range));

                if (user != null)
                    ShootLightning(user.Value, targetCoordinate, lightningPrototype, triggerLightningEvents);
                else
                    ShootLightning(coordinates, targetCoordinate, lightningPrototype, triggerLightningEvents);

                if (arcDepth > 0)
                {
                    ShootRandomLightnings(targetCoordinate, range, 1, lightningPrototype, arcDepth - 1, triggerLightningEvents, hitCoordsChance);
                }

                shootedCount++;
                continue;
            }

            if (outOfRange) { break; }
            // imp - random coordinate lightning end

            var curTarget = targets[count];
            if (!_random.Prob(curTarget.Comp.HitProbability)) //Chance to ignore target
                continue;

            // imp - use correct shoot lightning method based on whether this is coordinates-based or entity-based
            if (user != null)
                ShootLightning(user.Value, targets[count].Owner, lightningPrototype, triggerLightningEvents);
            else
                ShootLightning(coordinates, targets[count].Owner, lightningPrototype, triggerLightningEvents);

            if (arcDepth - targetLightningResistance > 0)
            {
                ShootRandomLightnings(targets[count].Owner, range, 1, lightningPrototype, arcDepth - targetLightningResistance, triggerLightningEvents, hitCoordsChance);
            }
            shootedCount++;
        }
    }

    /// <summary>
    /// Looks for objects with a LightningTarget component in the radius, prioritizes them, and hits the highest priority targets with lightning.
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="range">Targets selection radius</param>
    /// <param name="boltCount">Number of lightning bolts</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="arcDepth">how many times to recursively fire lightning bolts from the target points of the first shot.</param>
    /// <param name="triggerLightningEvents">if the lightnings being fired should trigger lightning events.</param>
    /// <param name="hitCoordsChance">Chance for lightning to strike random coordinates instead of an entity.</param>
    public void ShootRandomLightnings(EntityUid user, float range, int boltCount, string lightningPrototype = "Lightning", int arcDepth = 0, bool triggerLightningEvents = true, float hitCoordsChance = 0f)
    {
        // imp - Redirect to the other function for compatibility
        ShootRandomLightnings(_transform.GetMapCoordinates(user), range, boltCount, lightningPrototype, arcDepth, triggerLightningEvents, hitCoordsChance);
    }
}

/// <summary>
/// Raised directed on the target when an entity becomes the target of a lightning strike (not when touched)
/// </summary>
/// <param name="Source">The entity that created the lightning. May be null if the lightning came from coordinates rather than an entity.</param>
/// <param name="Target">The entity that was struck by lightning.</param>
[ByRefEvent]
public readonly record struct HitByLightningEvent(EntityUid? Source, EntityUid Target); // imp - Lightning might come from coordinates instead of an entity, so Source can be null

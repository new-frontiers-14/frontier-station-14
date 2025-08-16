using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared._NF.Fluids.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;


namespace Content.Server._NF.Fluids.EntitySystems;

public sealed class AdvDrainSystem : SharedDrainSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private readonly HashSet<Entity<PuddleComponent>> _puddles = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdvDrainComponent, MapInitEvent>(OnDrainMapInit);
        SubscribeLocalEvent<AdvDrainComponent, GetVerbsEvent<Verb>>(AddEmptyVerb);
        SubscribeLocalEvent<AdvDrainComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDrainMapInit(Entity<AdvDrainComponent> ent, ref MapInitEvent args)
    {
        // Randomise puddle drains so roundstart ones don't all dump at the same time.
        ent.Comp.Accumulator = _random.NextFloat(ent.Comp.DrainFrequency);
    }

    private void AddEmptyVerb(Entity<AdvDrainComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(args.Using, out SpillableComponent? spillable) ||
            !TryComp(args.Target, out AdvDrainComponent? drain))
            return;

        var used = args.Using.Value;
        var target = args.Target;
        Verb verb = new()
        {
            Text = Loc.GetString("drain-component-empty-verb-inhand", ("object", Name(used))),
            Act = () =>
            {
                Empty(used, spillable, target, drain);
            },
            Impact = LogImpact.Low,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))

        };
        args.Verbs.Add(verb);
    }

    private void Empty(EntityUid container, SpillableComponent spillable, EntityUid target, AdvDrainComponent drain)
    {
        // Find the solution in the container that is emptied
        if (!_solutionContainerSystem.TryGetDrainableSolution(container, out var containerSoln, out var containerSolution) || containerSolution.Volume == FixedPoint2.Zero)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-using-is-empty-message", ("object", container)),
                container);
            return;
        }

        // try to find the drain's solution
        if (!_solutionContainerSystem.ResolveSolution(target, AdvDrainComponent.SolutionName, ref drain.Solution, out var drainSolution))
        {
            return;
        }

        // Try to transfer as much solution as possible to the drain

        var amountToPutInDrain = drainSolution.AvailableVolume;
        var amountToSpillOnGround = containerSolution.Volume - drainSolution.AvailableVolume;

        if (amountToPutInDrain > 0)
        {
            var solutionToPutInDrain = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToPutInDrain);
            _solutionContainerSystem.TryAddSolution(drain.Solution.Value, solutionToPutInDrain);

            _audioSystem.PlayPvs(drain.ManualDrainSound, target);
            _ambientSoundSystem.SetAmbience(target, true);
        }


        // Don't actually spill the remainder.

        if (amountToSpillOnGround > 0)
        {
            // var solutionToSpill = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToSpillOnGround);
            // _puddleSystem.TrySpillAt(Transform(target).Coordinates, solutionToSpill, out _);
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-target-is-full-message", ("object", target)),
                container);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var managerQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        var query = EntityQueryEnumerator<AdvDrainComponent>();
        while (query.MoveNext(out var uid, out var drain))
        {
            // not anchored
            if (!TryComp(uid, out TransformComponent? xform) || !xform.Anchored)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsRunning, false);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsDraining, false);
                continue;
            }

            // not powered
            if (!_powerCell.HasCharge(uid, drain.Wattage))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsRunning, false);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsDraining, false);
                continue;
            }

            drain.Accumulator += frameTime;
            if (drain.Accumulator < drain.DrainFrequency)
            {
                continue;
            }
            drain.Accumulator -= drain.DrainFrequency;
            _appearanceSystem.SetData(uid, AdvDrainVisualState.IsRunning, true);

            // Disable ambient sound from emptying manually
            if (!drain.AutoDrain)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                continue;
            }

            if (!managerQuery.TryGetComponent(uid, out var manager))
                continue;

            // Best to do this one every second rather than once every tick...
            if (!_solutionContainerSystem.ResolveSolution((uid, manager), AdvDrainComponent.SolutionName, ref drain.Solution, out var drainSolution))
                continue;

            if (drainSolution.AvailableVolume <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                continue;
            }

            // Remove a bit from the buffer
            if (drainSolution.Volume > drain.UnitsDestroyedThreshold)
            {
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsVoiding, true);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsRunning, false); //they use the same indicator light, and cause artifacts when on at the same time
                _solutionContainerSystem.SplitSolution(drain.Solution.Value, Math.Min(drain.UnitsDestroyedPerSecond * drain.DrainFrequency, (float)drainSolution.Volume - drain.UnitsDestroyedThreshold));
            }
            else
            {
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsVoiding, false);
            }

            // This will ensure that UnitsPerSecond is per second...
            var amount = drain.UnitsPerSecond * drain.DrainFrequency;

            _puddles.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, drain.Range, _puddles);

            if (_puddles.Count == 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearanceSystem.SetData(uid, AdvDrainVisualState.IsDraining, false);
                continue;
            }

            _ambientSoundSystem.SetAmbience(uid, true);

            // only use power if it's actively draining puddles and isn't powered from an APC
            _powerCell.TryUseCharge(uid, drain.Wattage * drain.DrainFrequency);

            _appearanceSystem.SetData(uid, AdvDrainVisualState.IsDraining, true);
            amount /= _puddles.Count;

            foreach (var puddle in _puddles)
            {
                // Queue the solution deletion if it's empty. EvaporationSystem might also do this
                // but queuedelete should be pretty safe.
                if (!_solutionContainerSystem.ResolveSolution(puddle.Owner, puddle.Comp.SolutionName, ref puddle.Comp.Solution, out var puddleSolution))
                {
                    EntityManager.QueueDeleteEntity(puddle);
                    continue;
                }

                // Removes the lowest of:
                // the drain component's units per second adjusted for # of puddles
                // the puddle's remaining volume (making it cleanly zero)
                // the drain's remaining volume in its buffer.
                var transferSolution = _solutionContainerSystem.SplitSolution(puddle.Comp.Solution.Value,
                    FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.Volume, drainSolution.AvailableVolume));

                drainSolution.AddSolution(transferSolution, _prototypeManager);

                if (puddleSolution.Volume <= 0)
                {
                    QueueDel(puddle);
                }
            }

            _solutionContainerSystem.UpdateChemicals(drain.Solution.Value);
        }
    }

    private void OnExamined(Entity<AdvDrainComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !HasComp<SolutionContainerManagerComponent>(entity) ||
            !TryComp<AdvDrainComponent>(entity, out var drain) ||
            !_solutionContainerSystem.ResolveSolution(entity.Owner, AdvDrainComponent.SolutionName, ref entity.Comp.Solution, out var drainSolution))
        {
            return;
        }

        var text = Loc.GetString("adv-drain-component-examine-volume", ("volume", drainSolution.Volume), ("maxvolume", drain.UnitsDestroyedThreshold));
        args.PushMarkup(text);
    }
}

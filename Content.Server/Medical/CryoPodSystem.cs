using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent; // Frontier
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityEffects; // Frontier
using Content.Shared.Examine;
using Content.Shared.FixedPoint; // Frontier
using Content.Shared.Interaction;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.Power;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility; // Frontier

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly EntityManager _entManager = default!; // Frontier
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Frontier
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!; // Frontier

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, GotUnEmaggedEvent>(OnUnemagged); // Frontier
        SubscribeLocalEvent<CryoPodComponent, CryoPodDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, DragDropTargetEvent>(HandleDragDropOn);
        SubscribeLocalEvent<CryoPodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CryoPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryoPodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, ActivatableUIOpenAttemptEvent>(OnActivateUIAttempt);
        SubscribeLocalEvent<CryoPodComponent, AfterActivatableUIOpenEvent>(OnActivateUI);
        SubscribeLocalEvent<CryoPodComponent, EntRemovedFromContainerMessage>(OnEjected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var bloodStreamQuery = GetEntityQuery<BloodstreamComponent>();
        var bodyQuery = GetEntityQuery<BodyComponent>(); // Frontier
        var metaDataQuery = GetEntityQuery<MetaDataComponent>();
        var itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        var fitsInDispenserQuery = GetEntityQuery<FitsInDispenserComponent>();
        var solutionContainerManagerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            metaDataQuery.TryGetComponent(uid, out var metaDataComponent);
            if (curTime < cryoPod.NextInjectionTime + _metaDataSystem.GetPauseTime(uid, metaDataComponent))
                continue;
            cryoPod.NextInjectionTime = curTime + TimeSpan.FromSeconds(cryoPod.BeakerTransferTime);

            if (!itemSlotsQuery.TryGetComponent(uid, out var itemSlotsComponent))
            {
                continue;
            }
            var container = _itemSlotsSystem.GetItemOrNull(uid, cryoPod.SolutionContainerName, itemSlotsComponent);
            var patient = cryoPod.BodyContainer.ContainedEntity;
            if (container != null
                && container.Value.Valid
                && patient != null
                && fitsInDispenserQuery.TryGetComponent(container, out var fitsInDispenserComponent)
                && solutionContainerManagerQuery.TryGetComponent(container,
                    out var solutionContainerManagerComponent)
                && _solutionContainerSystem.TryGetFitsInDispenser((container.Value, fitsInDispenserComponent, solutionContainerManagerComponent),
                    out var containerSolution, out _))
            {
                // Frontier
                if (!bloodStreamQuery.TryGetComponent(patient, out var bloodstream))
                    continue;
                if (!bodyQuery.TryGetComponent(patient, out var body))
                    continue;

                var chemsToInject = new List<(string, FixedPoint2)>();
                if (cryoPod.ChemSmartInjectConditions != null)
                {
                    var conditionArgs = new EntityEffectBaseArgs(patient.Value, _entManager);
                    if (!cryoPod.AllowAllChems)
                    {
                        foreach (var (reagent, conditions) in cryoPod.ChemSmartInjectConditions)
                        {
                            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Id, out var proto))
                                DebugTools.Assert($"Bad ChemSmartInjectCondition reagent '{reagent.Id}'");
                            var dosage = cryoPod.MatchPatientMetabolism ?
                                _metabolizer.GetMetabolismRateForReagent((patient.Value, body), proto) / cryoPod.PotencyMultiplier :
                                cryoPod.BeakerTransferAmount;
                            if (conditions != null)
                            {
                                var allConditionsOK = true;
                                foreach (var cond in conditions)
                                {
                                    if (!cond.Condition(conditionArgs))
                                    {
                                        allConditionsOK = false;
                                        break;
                                    }
                                }
                                if (allConditionsOK)
                                {
                                    // All conditions satisfied - inject it
                                    chemsToInject.Add((reagent.Id, dosage));
                                }
                            }
                            else
                            {
                                // The reagent has no conditions - always inject it
                                chemsToInject.Add((reagent.Id, dosage));
                            }
                        }
                    }
                    else // cryoPod.AllowAllChems
                    {
                        foreach (var reagent in containerSolution.Value.Comp.Solution)
                        {
                            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var proto))
                                continue;
                            var dosage = cryoPod.MatchPatientMetabolism ?
                                _metabolizer.GetMetabolismRateForReagent((patient.Value, body), proto) / cryoPod.PotencyMultiplier :
                                cryoPod.BeakerTransferAmount;
                            chemsToInject.Add((reagent.Reagent.Prototype, dosage));
                        }
                    }
                }

                // Filter out a fixed amount of each reagent from the cryo pod's beaker
                var solutionToInject = _solutionContainerSystem.SplitSolutionPerReagentWithSpecificDoses(containerSolution.Value, chemsToInject.ToArray());

                // For every .25 units used, .5 units per second are added to the body, making cryo-pod more efficient than injections.
                solutionToInject.ScaleSolution(cryoPod.PotencyMultiplier);
                // End Frontier

                _bloodstreamSystem.TryAddToChemicals((patient.Value, bloodstream), solutionToInject);
                _reactiveSystem.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
            }
        }
    }

    public override EntityUid? EjectBody(EntityUid uid, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return null;
        if (cryoPodComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return null;
        base.EjectBody(uid, cryoPodComponent);
        // _climbSystem.ForciblySetClimbing(contained, uid); // Frontier
        return contained;
    }

    #region Interaction

    private void HandleDragDropOn(Entity<CryoPodComponent> entity, ref DragDropTargetEvent args)
    {
        if (entity.Comp.BodyContainer.ContainedEntity != null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, entity.Comp.EntryDelay, new CryoPodDragFinished(), entity, target: args.Dragged, used: entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(Entity<CryoPodComponent> entity, ref CryoPodDragFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (InsertBody(entity.Owner, args.Args.Target.Value, entity.Comp))
        {
            if (!TryComp(entity.Owner, out CryoPodAirComponent? cryoPodAir))
                _adminLogger.Add(LogType.Action, LogImpact.Medium,
                    $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(entity.Owner)}");

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(entity.Owner)} which contains gas: {cryoPodAir!.Air.ToPrettyString():gasMix}");
        }
        args.Handled = true;
    }

    private void OnActivateUIAttempt(Entity<CryoPodComponent> entity, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        var containedEntity = entity.Comp.BodyContainer.ContainedEntity;
        if (containedEntity == null || containedEntity == args.User || !HasComp<ActiveCryoPodComponent>(entity))
        {
            args.Cancel();
        }
    }

    private void OnActivateUI(Entity<CryoPodComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        if (!entity.Comp.BodyContainer.ContainedEntity.HasValue)
            return;

        TryComp<TemperatureComponent>(entity.Comp.BodyContainer.ContainedEntity, out var temp);
        TryComp<BloodstreamComponent>(entity.Comp.BodyContainer.ContainedEntity, out var bloodstream);

        if (TryComp<HealthAnalyzerComponent>(entity, out var healthAnalyzer))
        {
            healthAnalyzer.ScannedEntity = entity.Comp.BodyContainer.ContainedEntity;
        }

        // TODO: This should be a state my dude
        _uiSystem.ServerSendUiMessage(
            entity.Owner,
            HealthAnalyzerUiKey.Key,
            new HealthAnalyzerScannedUserMessage(GetNetEntity(entity.Comp.BodyContainer.ContainedEntity),
            temp?.CurrentTemperature ?? 0,
            (bloodstream != null && _solutionContainerSystem.ResolveSolution(entity.Comp.BodyContainer.ContainedEntity.Value,
                bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                ? bloodSolution.FillFraction
                : 0,
            null,
            null,
            null,
            null // Frontier
        ));
    }

    private void OnInteractUsing(Entity<CryoPodComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !entity.Comp.Locked || entity.Comp.BodyContainer.ContainedEntity == null)
            return;

        args.Handled = _toolSystem.UseTool(args.Used, args.User, entity.Owner, entity.Comp.PryDelay, PryingQuality, new CryoPodPryFinished());
    }

    private void OnExamined(Entity<CryoPodComponent> entity, ref ExaminedEvent args)
    {
        var container = _itemSlotsSystem.GetItemOrNull(entity.Owner, entity.Comp.SolutionContainerName);
        if (args.IsInDetailsRange && container != null && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var containerSolution))
        {
            using (args.PushGroup(nameof(CryoPodComponent)))
            {
                args.PushMarkup(Loc.GetString("cryo-pod-examine", ("beaker", Name(container.Value))));
                if (containerSolution.Volume == 0)
                {
                    args.PushMarkup(Loc.GetString("cryo-pod-empty-beaker"));
                }
            }
        }
    }

    private void OnPowerChanged(Entity<CryoPodComponent> entity, ref PowerChangedEvent args)
    {
        // Needed to avoid adding/removing components on a deleted entity
        if (Terminating(entity))
        {
            return;
        }

        if (args.Powered)
        {
            EnsureComp<ActiveCryoPodComponent>(entity);
        }
        else
        {
            RemComp<ActiveCryoPodComponent>(entity);
            _uiSystem.CloseUi(entity.Owner, HealthAnalyzerUiKey.Key);
        }
        UpdateAppearance(entity.Owner, entity.Comp);
    }

    #endregion

    #region Atmos handler

    private void OnCryoPodUpdateAtmosphere(Entity<CryoPodComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PortablePipeNode? portNode))
            return;

        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmosphereSystem.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryoPodComponent> entity, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(entity.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((entity.Comp.PortName, portAirLocal));
        }
    }

    private void OnEjected(Entity<CryoPodComponent> cryoPod, ref EntRemovedFromContainerMessage args)
    {
        if (TryComp<HealthAnalyzerComponent>(cryoPod.Owner, out var healthAnalyzer))
        {
            healthAnalyzer.ScannedEntity = null;
        }

        // if body is ejected - no need to display health-analyzer
        _uiSystem.CloseUi(cryoPod.Owner, HealthAnalyzerUiKey.Key);
    }

    #endregion
}

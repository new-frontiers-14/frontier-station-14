using System.Linq;
using Content.Server.Construction;
using Content.Server.GameTicking;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.M_Emp;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Chat.Managers;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.M_Emp
{
    public sealed partial class M_EmpSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Dependency] private readonly BiomeSystem _biome = default!;
        [Dependency] private readonly DungeonSystem _dungeon = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly ShuttleConsoleSystem _shuttleConsoles = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        // TODO: This is probably not compatible with multi-station
        private readonly Dictionary<EntityUid, M_EmpGridState> _M_EmpGridStates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<M_EmpGeneratorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<M_EmpGeneratorComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<M_EmpGeneratorComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<M_EmpGeneratorComponent, ExaminedEvent>(OnExamined);
//            SubscribeLocalEvent<M_EmpGeneratorComponent, ComponentShutdown>(OnGeneratorRemoval);
//            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

            // Can't use RoundRestartCleanupEvent, I need to clean up before the grid, and components are gone to prevent the announcements
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);

//            InitializeExpeditions();
//            InitializeRunner();
        }

        public override void Shutdown()
        {
            base.Shutdown();
//            ShutdownExpeditions();
        }

        private void OnRoundEnd(GameRunLevelChangedEvent ev)
        {
            if(ev.New != GameRunLevel.InRound)
            {
 //               _M_EmpGridStates.Clear();
            }
        }

        private void UpdateAppearance(EntityUid uid, M_EmpGeneratorComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            _appearanceSystem.SetData(uid, M_EmpGeneratorVisuals.ReadyBlinking, component.GeneratorState.StateType == GeneratorStateType.Attaching);
            _appearanceSystem.SetData(uid, M_EmpGeneratorVisuals.Ready, component.GeneratorState.StateType == GeneratorStateType.Holding);
            _appearanceSystem.SetData(uid, M_EmpGeneratorVisuals.Unready, component.GeneratorState.StateType == GeneratorStateType.CoolingDown);
            _appearanceSystem.SetData(uid, M_EmpGeneratorVisuals.UnreadyBlinking, component.GeneratorState.StateType == GeneratorStateType.Detaching);
        }

        private void UpdateChargeStateAppearance(EntityUid uid, TimeSpan currentTime, M_EmpGeneratorComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            var timeLeft = Convert.ToInt32(component.GeneratorState.Until.TotalSeconds - currentTime.TotalSeconds);

            component.ChargeRemaining = component.GeneratorState.StateType switch
            {
                GeneratorStateType.Inactive => 5,
                GeneratorStateType.Holding => timeLeft / (Convert.ToInt32(component.HoldTime.TotalSeconds) / component.ChargeCapacity) + 1,
                GeneratorStateType.Detaching => 0,
                GeneratorStateType.CoolingDown => component.ChargeCapacity - timeLeft / (Convert.ToInt32(component.CooldownTime.TotalSeconds) / component.ChargeCapacity) - 1,
                _ => component.ChargeRemaining
            };

            if (component.PreviousCharge == component.ChargeRemaining)
                return;
            _appearanceSystem.SetData(uid, M_EmpGeneratorVisuals.ChargeState, component.ChargeRemaining);
            component.PreviousCharge = component.ChargeRemaining;
        }

//        private void OnGridRemoval(GridRemovalEvent ev)
//        {
//            // If we ever want to give generators names, and announce them individually, we would need to loop this, before removing it.
//            if (_M_EmpGridStates.Remove(ev.EntityUid))
//            {
//                if (TryComp<M_EmpGridComponent>(ev.EntityUid, out var salvComp) &&
//                    TryComp<M_EmpGeneratorComponent>(salvComp.SpawnerGenerator, out var generator))
//                    Report(salvComp.SpawnerGenerator.Value, generator.M_EmpChannel, "M_Emp-system-announcement-spawn-generator-lost");
//                // For the very unlikely possibility that the M_Emp generator was on a M_Emp, we will not return here
//            }
//            foreach(var gridState in _M_EmpGridStates)
//            {
//                foreach(var generator in gridState.Value.ActiveGenerators)
//                {
//                    if (!TryComp<M_EmpGeneratorComponent>(generator, out var generatorComponent))
//                        continue;

//                    if (generatorComponent.AttachedEntity != ev.EntityUid)
//                        continue;
//                    generatorComponent.AttachedEntity = null;
//                    generatorComponent.GeneratorState = GeneratorState.Inactive;
//                    return;
//                }
//            }
//        }

//        private void OnGeneratorRemoval(EntityUid uid, M_EmpGeneratorComponent component, ComponentShutdown args)
//        {
//            if (component.GeneratorState.StateType == GeneratorStateType.Inactive)
//                return;
//
//            var generatorTranform = Transform(uid);
//            if (generatorTranform.GridUid is not { } gridId || !_M_EmpGridStates.TryGetValue(gridId, out var M_EmpGridState))
//                return;
//
//            M_EmpGridState.ActiveGenerators.Remove(uid);
//            Report(uid, component.M_EmpChannel, "M_Emp-system-announcement-spawn-generator-lost");
//            if (component.AttachedEntity.HasValue)
//            {
//                SafeDeleteM_Emp(component.AttachedEntity.Value);
//                component.AttachedEntity = null;
//                Report(uid, component.M_EmpChannel, "M_Emp-system-announcement-lost");
//            }
//            else if (component.GeneratorState is { StateType: GeneratorStateType.Attaching })
//            {
//                Report(uid, component.M_EmpChannel, "M_Emp-system-announcement-spawn-no-debris-available");
//            }
//
//            component.GeneratorState = GeneratorState.Inactive;
//        }

        private void OnRefreshParts(EntityUid uid, M_EmpGeneratorComponent component, RefreshPartsEvent args)
        {
            var rating = args.PartRatings[component.MachinePartDelay] - 1;
            var factor = MathF.Pow(component.PartRatingDelay, rating);
            component.AttachingTime = component.BaseAttachingTime * factor;
            component.CooldownTime = component.BaseCooldownTime * factor;
        }

        private void OnUpgradeExamine(EntityUid uid, M_EmpGeneratorComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("m_emp-system-generator-delay-upgrade", (float) (component.CooldownTime / component.BaseCooldownTime));
        }

        private void OnExamined(EntityUid uid, M_EmpGeneratorComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var gotGrid = false;
            var remainingTime = TimeSpan.Zero;

//            if (Transform(uid).GridUid is { } gridId &&
//                _M_EmpGridStates.TryGetValue(gridId, out var M_EmpGridState))
//            {
//                remainingTime = component.GeneratorState.Until - M_EmpGridState.CurrentTime;
//                gotGrid = true;
//            }
//            else
//            {
//                Log.Warning("Failed to load M_Emp grid state, can't display remaining time");
//            }

            switch (component.GeneratorState.StateType)
            {
                case GeneratorStateType.Inactive:
                    args.PushMarkup(Loc.GetString("m_emp-system-generator-examined-inactive"));
                    break;
                case GeneratorStateType.Attaching:
                    args.PushMarkup(Loc.GetString("m_emp-system-generator-examined-pulling-in"));
                    break;
                case GeneratorStateType.Detaching:
                    args.PushMarkup(Loc.GetString("m_emp-system-generator-examined-releasing"));
                    break;
                case GeneratorStateType.CoolingDown:
                    if (gotGrid)
                        args.PushMarkup(Loc.GetString("m_emp-system-generator-examined-cooling-down", ("timeLeft", Math.Ceiling(remainingTime.TotalSeconds))));
                    break;
                case GeneratorStateType.Holding:
                    if (gotGrid)
                        args.PushMarkup(Loc.GetString("m_emp-system-generator-examined-active", ("timeLeft", Math.Ceiling(remainingTime.TotalSeconds))));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnInteractHand(EntityUid uid, M_EmpGeneratorComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = true;
            StartGenerator(uid, component, args.User);
            UpdateAppearance(uid, component);
        }

        private void StartGenerator(EntityUid uid, M_EmpGeneratorComponent component, EntityUid user)
        {
            switch (component.GeneratorState.StateType)
            {
                case GeneratorStateType.Inactive:
                    ShowPopup(uid, "m_emp-system-report-activate-success", user);
                    var generatorTransform = Transform(uid);
                    var gridId = generatorTransform.GridUid ?? throw new InvalidOperationException("Generator had no grid associated");
                    if (!_M_EmpGridStates.TryGetValue(gridId, out var gridState))
                    {
                        gridState = new M_EmpGridState();
                        _M_EmpGridStates[gridId] = gridState;
                    }
                    gridState.ActiveGenerators.Add(uid);
                    component.GeneratorState = new GeneratorState(GeneratorStateType.Attaching, gridState.CurrentTime + component.AttachingTime);
                    RaiseLocalEvent(new M_EmpGeneratorActivatedEvent(uid));
                    Report(uid, component.M_EmpChannel, "m_emp-system-report-activate-success");
                    break;
                case GeneratorStateType.Attaching:
                case GeneratorStateType.Holding:
                    ShowPopup(uid, "m_emp-system-report-already-active", user);
                    break;
                case GeneratorStateType.Detaching:
                case GeneratorStateType.CoolingDown:
                    ShowPopup(uid, "m_emp-system-report-cooling-down", user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void ShowPopup(EntityUid uid, string messageKey, EntityUid user)
        {
            _popupSystem.PopupEntity(Loc.GetString(messageKey), uid, user);
        }

        private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
        {
            var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
            var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
            _radioSystem.SendRadioMessage(source, message, channel, source);
        }

        private void Transition(EntityUid uid, M_EmpGeneratorComponent generator, TimeSpan currentTime)
        {
            switch (generator.GeneratorState.StateType)
            {
                case GeneratorStateType.Attaching:
//                    if (SpawnM_Emp(uid, generator))
//                    {
//                        generator.GeneratorState = new GeneratorState(GeneratorStateType.Holding, currentTime + generator.HoldTime);
//                    }
//                    else
//                    {
//                        generator.GeneratorState = new GeneratorState(GeneratorStateType.CoolingDown, currentTime + generator.CooldownTime);
//                    }
                    break;
                case GeneratorStateType.Holding:
                    Report(uid, generator.M_EmpChannel, "m_emp-system-announcement-losing", ("timeLeft", generator.DetachingTime.TotalSeconds));
                    generator.GeneratorState = new GeneratorState(GeneratorStateType.Detaching, currentTime + generator.DetachingTime);
                    break;
                case GeneratorStateType.Detaching:
 //                   if (generator.AttachedEntity.HasValue)
 //                   {
 //                       SafeDeleteM_Emp(generator.AttachedEntity.Value);
 //                   }
 //                   else
 //                   {
 //                       Log.Error("M_Emp detaching was expecting attached entity but it was null");
 //                   }
                    Report(uid, generator.M_EmpChannel, "m_emp-system-announcement-lost");
                    generator.GeneratorState = new GeneratorState(GeneratorStateType.CoolingDown, currentTime + generator.CooldownTime);
                    break;
                case GeneratorStateType.CoolingDown:
                    generator.GeneratorState = GeneratorState.Inactive;
                    break;
            }
            UpdateAppearance(uid, generator);
            UpdateChargeStateAppearance(uid, currentTime, generator);
        }

        public override void Update(float frameTime)
        {
            var secondsPassed = TimeSpan.FromSeconds(frameTime);
            // Keep track of time, and state per grid
            foreach (var (uid, state) in _M_EmpGridStates)
            {
                if (state.ActiveGenerators.Count == 0) continue;
                // Not handling the case where the M_Emp we spawned got paused
                // They both need to be paused, or it doesn't make sense
                if (MetaData(uid).EntityPaused) continue;
                state.CurrentTime += secondsPassed;

                var deleteQueue = new RemQueue<EntityUid>();

                foreach(var generator in state.ActiveGenerators)
                {
                    if (!TryComp<M_EmpGeneratorComponent>(generator, out var generatorComp))
                        continue;

                    UpdateChargeStateAppearance(generator, state.CurrentTime, generatorComp);
                    if (generatorComp.GeneratorState.Until > state.CurrentTime) continue;
                    Transition(generator, generatorComp, state.CurrentTime);
                    if (generatorComp.GeneratorState.StateType == GeneratorStateType.Inactive)
                    {
                        deleteQueue.Add(generator);
                    }
                }

                foreach(var generator in deleteQueue)
                {
                    state.ActiveGenerators.Remove(generator);
                }
            }

//            UpdateExpeditions();
//            UpdateRunner();
        }
    }

    public sealed class M_EmpGridState
    {
        public TimeSpan CurrentTime { get; set; }
        public List<EntityUid> ActiveGenerators { get; } = new();
    }
}


using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Content.Shared.Carrying;

namespace Content.Shared.LieDown;

public class SharedLieDownSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LyingDownComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<LyingDownComponent, GetVerbsEvent<AlternativeVerb>>(AddStandUpVerb);
        SubscribeLocalEvent<LyingDownComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LyingDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);

        SubscribeLocalEvent<LyingDownComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<LyingDownComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<LyingDownComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<LyingDownComponent, CarryDoAfterEvent>(OnDoAfter);


        // Bind keybinds to lie down action
        SubscribeNetworkEvent<ChangeStandingStateEvent>(OnChangeAction);
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.LieDownStandUp, InputCmdHandler.FromDelegate(ChangeLyingState))
            .Register<SharedLieDownSystem>();
    }

    private void OnDoAfter(EntityUid uid, LyingDownComponent component, CarryDoAfterEvent args) {
        _standing.Stand(uid);
        RemCompDeferred<LyingDownComponent>(uid);
    }

    private void OnBuckleChange(EntityUid uid, LyingDownComponent component, ref BuckleChangeEvent args) {
        if (args.Buckling) {
            RemCompDeferred<LyingDownComponent>(uid);
        }
    }

    private void OnComponentShutdown(EntityUid uid, LyingDownComponent component, ComponentShutdown args)
    {
        SwitchActions(uid);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnComponentStartup(EntityUid uid, LyingDownComponent component, ComponentStartup args)
    {
        SwitchActions(uid);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    ///     Send an update event when player pressed keybind.
    /// </summary>
    private void ChangeLyingState(ICommonSession? session)
    {
        RaiseNetworkEvent(new ChangeStandingStateEvent());
    }

    /// <summary>
    ///     Process player event, that pressed keybind.
    /// </summary>
    private void OnChangeAction(ChangeStandingStateEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return;

        var uid = args.SenderSession.AttachedEntity.Value;
        if (_standing.IsDown(uid))
        {
            TryStandUp(uid);
        }
        else
        {
            TryLieDown(uid);
        }
    }

    /// <summary>
    ///     Update movement speed according to the lying state.
    /// </summary>
    private void OnRefresh(EntityUid uid, LyingDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_standing.IsDown(uid))
        {
            args.ModifySpeed(0f, 0f);
        }
        else
        {
            args.ModifySpeed(1f, 1f);
        }
    }

    /// <summary>
    ///     Change available to player actions.
    /// </summary>
    private void SwitchActions(EntityUid uid)
    {
        var standingComponent = Comp<StandingStateComponent>(uid);
        if (_standing.IsDown(uid))
        {
            _actions.AddAction(uid, ref standingComponent.StandUpActionEntity, standingComponent.StandUpAction);
            _actions.RemoveAction(uid, standingComponent.LieDownActionEntity);
        }
        else
        {
            _actions.AddAction(uid, ref standingComponent.LieDownActionEntity, standingComponent.LieDownAction);
            _actions.RemoveAction(uid, standingComponent.StandUpActionEntity);
        }
    }

    /// <summary>
    ///     When interacting with a lying down person, add ability to make him stand up.
    /// </summary>
    private void OnInteractHand(EntityUid uid, LyingDownComponent component, InteractHandEvent args)
    {
        TryStandUp(args.Target);
    }

    /// <summary>
    ///     Add a verb to player menu to make him stand up.
    /// </summary>
    private void AddStandUpVerb(EntityUid uid, LyingDownComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (args.Target == args.User)
            return;

        if (!_standing.IsDown(uid))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryStandUp(uid);
            },
            Text = Loc.GetString(component.MakeToStandUpAction!),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     If somebody examined a lying down person, add description.
    /// </summary>
    private void OnExamined(EntityUid uid, LyingDownComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && _standing.IsDown(uid))
        {
            args.PushMarkup(Loc.GetString("lying-down-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    public void TryStandUp(EntityUid uid)
    {
        if (!_standing.IsDown(uid) || !_standing.Stand(uid))
            return;

        RemCompDeferred<LyingDownComponent>(uid);
    }

    public void TryLieDown(EntityUid uid)
    {
        if (_standing.IsDown(uid) || !_standing.Down(uid, false, false))
            return;

        EnsureComp<LyingDownComponent>(uid);
    }
}


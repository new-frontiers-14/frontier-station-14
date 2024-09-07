using Content.Server.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage;
using Content.Server.Carrying; // Carrying system from Nyanotrasen.
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Resist;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Content.Server.Storage.Components;
using Content.Server.Carrying;
using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly CarryingSystem _carryingSystem = default!; // Carrying system from Nyanotrasen.
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private  readonly EntityManager _entityManager = default!;

    // Frontier - cancel inventory escape
    [ValidatePrototypeId<EntityPrototype>]
    private readonly string _escapeCancelAction = "ActionCancelEscape";

    /// <summary>
    /// You can't escape the hands of an entity this many times more massive than you.
    /// </summary>
    public const float MaximumMassDisadvantage = 6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryCancelActionEvent>(OnCancelEscape); // Frontier
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!_containerSystem.TryGetContainingContainer((uid, null, null), out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        if (args.OldMovement == MoveButtons.None || args.OldMovement == MoveButtons.Walk)
            return; // This event gets fired when the user holds down shift, which makes no sense

        // Make sure there's nothing stopped the removal (like being glued)
        if (!_containerSystem.CanRemove(uid, container))
        {
            _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), uid, uid);
            return;
        }

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out _))
        {
            AttemptEscape(uid, container.Owner, component);
            return;
        }

        // Uncontested
        if (HasComp<StorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SecretStashComponent>(container.Owner))
            AttemptEscape(uid, container.Owner, component);
    }

    public void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f) //private to public for carrying system.
    {
        if (component.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.BaseResistTime * multiplier, new EscapeInventoryEvent(), user, target: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out component.DoAfter))
            return;

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);

        // Frontier - escape cancel action
        if (component.EscapeCancelAction is not { Valid: true })
            _actions.AddAction(user, ref component.EscapeCancelAction, _escapeCancelAction);
    }

    private void OnEscape(EntityUid uid, CanEscapeInventoryComponent component, EscapeInventoryEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        RemoveCancelAction(uid, component); // Frontier

        if (TryComp<BeingCarriedComponent>(uid, out var carried)) // Start of carrying system of nyanotrasen.
        {
            _carryingSystem.DropCarried(carried.Carrier, uid);
            return;
        } // End of carrying system of nyanotrasen.

        _containerSystem.AttachParentToContainerOrGrid((uid, Transform(uid)));
        args.Handled = true;
    }

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        if (component.DoAfter != null)
            _doAfterSystem.Cancel(component.DoAfter);

        RemoveCancelAction(uid, component); // Frontier
    }

    // Frontier
    private void RemoveCancelAction(EntityUid uid, CanEscapeInventoryComponent component)
    {
        if (component.EscapeCancelAction is not { Valid: true })
         return;

        _actions.RemoveAction(uid, component.EscapeCancelAction);
        component.EscapeCancelAction = null;
    }

    // Frontier
    private void OnCancelEscape(EntityUid uid, CanEscapeInventoryComponent component, EscapeInventoryCancelActionEvent args)
    {
        if (component.DoAfter != null)
            _doAfterSystem.Cancel(component.DoAfter);

        RemoveCancelAction(uid, component);
    }
}

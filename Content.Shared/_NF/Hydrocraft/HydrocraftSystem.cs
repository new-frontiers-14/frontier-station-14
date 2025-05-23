using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.Nutrition.Components;
using Content.Shared.Stacks;

namespace Content.Shared._NF.Hydrocraft;

/// <summary>
/// Allows mobs to produce materials with <see cref="HydrocraftComponent"/>.
/// </summary>
public abstract partial class SharedHydrocraftSystem : EntitySystem
{
    // Managers
    [Dependency] private readonly INetManager _netManager = default!;

    // Systems
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HydrocraftComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HydrocraftComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<HydrocraftComponent, HydrocraftActionEvent>(OnHydrocraftStart);
        SubscribeLocalEvent<HydrocraftComponent, HydrocraftDoAfterEvent>(OnHydrocraftDoAfter);
    }

    /// <summary>
    /// Giveths the action to preform sericulture on the entity
    /// </summary>
    private void OnMapInit(EntityUid uid, HydrocraftComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    /// <summary>
    /// Takeths away the action to preform sericulture from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, HydrocraftComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnHydrocraftStart(EntityUid uid, HydrocraftComponent comp, HydrocraftActionEvent args)
    {
        if (TryComp<ThirstComponent>(uid, out var thirstComp)
            && _thirstSystem.IsThirstBelowState(uid,
                comp.MinThirstThreshold,
                _thirstSystem.GetThirst(thirstComp) - comp.ThirstCost,
                thirstComp))
        {
            _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, comp.ProductionLength, new HydrocraftDoAfterEvent(), uid)
        { // I'm not sure if more things should be put here, but imo ideally it should probably be set in the component/YAML. Not sure if this is currently possible.
            BreakOnMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }


    private void OnHydrocraftDoAfter(EntityUid uid, HydrocraftComponent comp, HydrocraftDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        if (TryComp<ThirstComponent>(uid,
                out var thirstComp) // A check, just incase the doafter is somehow performed when the entity is not in the right thirst state.
            && _thirstSystem.IsThirstBelowState(uid,
                comp.MinThirstThreshold,
                _thirstSystem.GetThirst(thirstComp) - comp.ThirstCost,
                thirstComp))
        {
            _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        _thirstSystem.ModifyThirst(uid, thirstComp!, -comp.ThirstCost);

        if (!_netManager.IsClient) // Have to do this because spawning stuff in shared is CBT.
        {
            var newEntity = Spawn(comp.EntityProduced, Transform(uid).Coordinates);

            _stackSystem.TryMergeToHands(newEntity, uid);
        }

        args.Repeat = true;
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class HydrocraftActionEvent : InstantActionEvent { }

/// <summary>
/// Is relayed at the end of the sericulturing doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HydrocraftDoAfterEvent : SimpleDoAfterEvent { }


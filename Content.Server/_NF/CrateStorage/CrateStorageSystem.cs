using Content.Server._NF.CrateMachine;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._NF.CrateStorage;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._NF.CrateStorage;

/// <summary>
/// This file is responsible for overriding the Initialize function, provide all the systems, and subscribing to events.
/// This keeps that clutter out of the individual partial classes.
/// </summary>
public sealed partial class CrateStorageSystem: SharedCrateStorageMachineSystem
{

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly CrateMachineSystem _crateMachineSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrateStorageMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CrateStorageMachineComponent, ItemPlacedEvent>(OnItemPlacedEvent);
        SubscribeLocalEvent<CrateStorageMachineComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<CrateStorageMachineComponent, CrateMachineOpenedEvent>(OnCrateMachineOpened);
        SubscribeLocalEvent<CrateStorageRackComponent, PowerChangedEvent>(OnRackPowerChanged);
    }

    private void OnInit(EntityUid uid, CrateStorageMachineComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.TriggerPort);
    }
}

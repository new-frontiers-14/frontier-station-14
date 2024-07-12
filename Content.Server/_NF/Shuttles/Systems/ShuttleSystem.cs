// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Server._NF.Station.Components;
using Content.Server.Shuttles.Components;
using Content.Shared._NF.Shuttles.Events;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private const float AnchorDampeningStrength = 1.0f;
    private void NfInitialize()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, ToggleStabilizerRequest>(OnToggleStabilizer);
    }

    private void OnToggleStabilizer(EntityUid uid, ShuttleConsoleComponent component, ToggleStabilizerRequest args)
    {
        // Ensure that the entity requested is a valid shuttle (stations should not be togglable)
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) ||
            !transform.GridUid.HasValue ||
            !EntityManager.TryGetComponent(transform.GridUid, out PhysicsComponent? physicsComponent) ||
            !EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttleComponent) ||
            EntityManager.HasComponent<StationDampeningComponent>(_station.GetOwningStation(transform.GridUid)))
        {
            return;
        }

        var linearDampeningStrength = args.Mode switch
        {
            InertiaDampeningMode.Off => 0,
            InertiaDampeningMode.Dampen => shuttleComponent.LinearDamping,
            InertiaDampeningMode.Anchor => AnchorDampeningStrength,
            _ => 0, // other values: default to some sane behaviour (assume all dampening is off)
        };

        var angularDampeningStrength = args.Mode switch
        {
            InertiaDampeningMode.Off => 0,
            InertiaDampeningMode.Dampen => shuttleComponent.AngularDamping,
            InertiaDampeningMode.Anchor => AnchorDampeningStrength,
            _ => 0, // other values: default to some sane behaviour (assume all dampening is off)
        };

        _physics.SetLinearDamping(transform.GridUid.Value, physicsComponent, linearDampeningStrength);
        _physics.SetAngularDamping(transform.GridUid.Value, physicsComponent, angularDampeningStrength);
        _console.RefreshShuttleConsoles(transform.GridUid.Value);
    }

    public InertiaDampeningMode NfGetInertiaDampeningMode(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(entity, out var xform))
            return InertiaDampeningMode.Off;

        if (EntityManager.HasComponent<StationDampeningComponent>(_station.GetOwningStation(xform.GridUid)))
            return InertiaDampeningMode.Station;

        if (!EntityManager.TryGetComponent(xform.GridUid, out PhysicsComponent? physicsComponent))
            return InertiaDampeningMode.Off;

        if (physicsComponent.LinearDamping >= AnchorDampeningStrength)
            return InertiaDampeningMode.Anchor;
        else if (MathHelper.CloseTo(physicsComponent.LinearDamping, 0.0f))
            return InertiaDampeningMode.Off;
        else
            return InertiaDampeningMode.Dampen;
    }

}

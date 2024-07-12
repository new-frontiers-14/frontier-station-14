// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Server.Shuttles.Components;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared._NF.Station.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Physics.Components;
using SixLabors.ImageSharp.ColorSpaces;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
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
            InertiaDampeningMode.Anchor => 1,
            _ => 0, // other values: default to some sane behaviour (assume all dampening is off)
        };

        var angularDampeningStrength = args.Mode switch
        {
            InertiaDampeningMode.Off => 0,
            InertiaDampeningMode.Dampen => shuttleComponent.AngularDamping,
            InertiaDampeningMode.Anchor => 1,
            _ => 0, // other values: default to some sane behaviour (assume all dampening is off)
        };

        _physics.SetLinearDamping(transform.GridUid.Value, physicsComponent, linearDampeningStrength);
        _physics.SetAngularDamping(transform.GridUid.Value, physicsComponent, angularDampeningStrength);
    }

}

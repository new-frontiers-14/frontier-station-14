using Content.Server.Shuttles.Components;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared._NF.Station.Components;
using Content.Shared.Database;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Physics.Components;
using SixLabors.ImageSharp.ColorSpaces;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private const float AnchorDampeningStrength = 1.0f;
    private const int StationaryAnchorStrength = 999999;

    private void NfInitialize()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, ToggleStabilizerRequest>(OnToggleStabilizer);
    }

    private void OnToggleStabilizer(EntityUid uid, ShuttleConsoleComponent component, ToggleStabilizerRequest args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) ||
            !transform.GridUid.HasValue ||
            !EntityManager.TryGetComponent(transform.GridUid, out PhysicsComponent? physicsComponent) ||
            !EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttleComponent) ||
            EntityManager.HasComponent<StationComponent>(transform.GridUid))
        {
            return;
        }

        var linearDampeningStrength = args.Mode switch
        {
            InertiaDampeningMode.Off => 0,
            InertiaDampeningMode.Dampen => shuttleComponent.LinearDamping,
            InertiaDampeningMode.Anchored => AnchorDampeningStrength,
            _ => StationaryAnchorStrength,// if it's not the above it's a station, they shouldn't be able to change the dampening, should we alert the admin?
        };

        var angularDampeningStrength = args.Mode switch
        {
            InertiaDampeningMode.Off => 0,
            InertiaDampeningMode.Dampen => shuttleComponent.AngularDamping,
            InertiaDampeningMode.Anchored => AnchorDampeningStrength,
            _ => StationaryAnchorStrength,// if it's not the above it's a station, they shouldn't be able to change the dampening, should we alert the admin?
        };

        _physics.SetLinearDamping(transform.GridUid.Value, physicsComponent, linearDampeningStrength);
        _physics.SetAngularDamping(transform.GridUid.Value, physicsComponent, angularDampeningStrength);
        _logger.Add(LogType.Action,LogImpact.Medium, $"{ToPrettyString(player):player} set {ToPrettyString(uid)} dampeners to {args.Mode}.");
    }

}

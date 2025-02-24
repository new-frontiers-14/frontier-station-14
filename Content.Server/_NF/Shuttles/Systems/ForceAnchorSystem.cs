using Content.Server._NF.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._NF.Shuttles.Systems;

public sealed partial class ForceAnchorSystem : EntitySystem
{
    [Dependency] PhysicsSystem _physics = default!;
    [Dependency] ShuttleSystem _shuttle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ForceAnchorComponent, MapInitEvent>(OnForceAnchorMapInit);
        SubscribeLocalEvent<ForceAnchorPostFTLComponent, FTLCompletedEvent>(OnForceAnchorPostFTLCompleted);
    }

    private void OnForceAnchorMapInit(Entity<ForceAnchorComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<PhysicsComponent>(ent, out var physics))
        {
            _physics.SetBodyType(ent, BodyType.Static, body: physics);
            _physics.SetBodyStatus(ent, physics, BodyStatus.OnGround);
            _physics.SetFixedRotation(ent, true, body: physics);
        }
        _shuttle.Disable(ent);
        EnsureComp<PreventGridAnchorChangesComponent>(ent);
    }

    private void OnForceAnchorPostFTLCompleted(Entity<ForceAnchorPostFTLComponent> ent, ref FTLCompletedEvent args)
    {
        if (TryComp<PhysicsComponent>(ent, out var physics))
        {
            _physics.SetBodyType(ent, BodyType.Static, body: physics);
            _physics.SetBodyStatus(ent, physics, BodyStatus.OnGround);
            _physics.SetFixedRotation(ent, true, body: physics);
        }
        _shuttle.Disable(ent);
        EnsureComp<PreventGridAnchorChangesComponent>(ent);
    }
}

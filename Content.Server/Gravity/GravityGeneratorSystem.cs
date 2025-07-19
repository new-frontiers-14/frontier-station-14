using Content.Server.Popups; // Frontier
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Construction.Components; // Frontier
using Content.Shared.Gravity;
using Content.Shared.Tools.Components; // Frontier
using Content.Shared.Popups; // Frontier

namespace Content.Server.Gravity;

public sealed class GravityGeneratorSystem : EntitySystem
{
    [Dependency] private readonly GravitySystem _gravitySystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    [Dependency] private readonly PopupSystem _popupSystem = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
        // SubscribeLocalEvent<GravityGeneratorComponent, EmpPulseEvent>(OnEmpPulse); // Frontier: Upstream - #28984
        SubscribeLocalEvent<GravityGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt); // Frontier
        SubscribeLocalEvent<GravityGeneratorComponent, ToolUseAttemptEvent>(OnToolUseAttempt); // Frontier
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GravityGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
        }
    }

    private void OnActivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.GravityActive = true;

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        }
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.GravityActive = false;

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
        }
    }

    /// <summary>
    /// Frontier: Prevent unanchoring when anchor is active
    /// </summary>
    private void OnUnanchorAttempt(Entity<GravityGeneratorComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (!ent.Comp.GravityActive)
            return;

        _popupSystem.PopupEntity(
            Loc.GetString("gravity-generator-unanchoring-failed"),
            ent,
            args.User,
            PopupType.Medium);

        args.Cancel();
    }

    /// <summary>
    /// Frontier: Prevent disassembly when anchor is active
    /// </summary>
    private void OnToolUseAttempt(Entity<GravityGeneratorComponent> ent, ref ToolUseAttemptEvent args)
    {
        if (!ent.Comp.GravityActive)
            return;

        foreach (var quality in args.Qualities)
        {
            // prevent reconstruct
            if (quality == "Prying")
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("gravity-generator-teardown-failed"),
                     ent,
                     args.User,
                     PopupType.Medium);

                args.Cancel();
                return;
            }
        }
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
        }
    }
}

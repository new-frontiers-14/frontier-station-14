using Content.Server.Humanoid;
using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;

namespace Content.Server._Starlight.Medical.Surgery;

public sealed partial class OrganSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganImplantationCompleted>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganExtracted>(OnFunctionalOrganExtracted);
        SubscribeLocalEvent<FunctionalOrganComponent, OrganAddedToBodyEvent>(OnFunctionalOrganAddedToBody);
        SubscribeLocalEvent<FunctionalOrganComponent, OrganRemovedFromBodyEvent>(OnFunctionalOrganRemovedFromBody);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtracted>(OnEyeExtracted);

        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtracted>(OnTongueExtracted);

        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtracted>(OnOrganExtracted);

        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganImplantationCompleted>(OnVisualizationImplanted);
        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganExtracted>(OnVisualizationExtracted);
    }

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent,
        ref SurgeryOrganImplantationCompleted args)
    {
        ApplyFunctionalOrgan(args.Body, ent.Comp);
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        RemoveFunctionalOrgan(args.Body, ent.Comp);
    }

    private void OnFunctionalOrganAddedToBody(Entity<FunctionalOrganComponent> ent, ref OrganAddedToBodyEvent args)
    {
        ApplyFunctionalOrgan(args.Body, ent.Comp);
    }

    private void OnFunctionalOrganRemovedFromBody(Entity<FunctionalOrganComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        RemoveFunctionalOrgan(args.OldBody, ent.Comp);
    }

    private void ApplyFunctionalOrgan(EntityUid body, FunctionalOrganComponent comp)
    {
        foreach (var item in (comp.Components ?? []).Values)
        {
            if (!EntityManager.HasComponent(body, item.Component.GetType()))
                EntityManager.AddComponent(body, _compFactory.GetComponent(item.Component.GetType()));
        }
    }

    private void RemoveFunctionalOrgan(EntityUid body, FunctionalOrganComponent comp)
    {
        foreach (var item in (comp.Components ?? []).Values)
        {
            if (EntityManager.HasComponent(body, item.Component.GetType()))
                EntityManager.RemoveComponent(body, _compFactory.GetComponent(item.Component.GetType()));
        }
    }

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<DamageableComponent>(args.Body, out var bodyDamageable))
            return;

        var change = _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage, true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, InvertDamage(change), true, false, ent.Comp);
    }

    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
            || damageRule.Damage is null
            || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable))
            return;

        var change = _damageableSystem.TryChangeDamage(args.Body, InvertDamage(damageRule.Damage), true, false,
            bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, InvertDamage(change), true, false, ent.Comp);
    }

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!ent.Comp.IsMuted)
            return;
        RemComp<MutedComponent>(args.Body);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtracted args)
    {
        ent.Comp.IsMuted = HasComp<MutedComponent>(args.Body);
        AddComp<MutedComponent>(args.Body);
    }

    private void OnEyeExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        ent.Comp.EyeDamage = blindable.EyeDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }

    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }

    private void OnVisualizationExtracted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganExtracted args)
        => _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], false);

    private void OnVisualizationImplanted(Entity<OrganVisualizationComponent> ent,
        ref SurgeryOrganImplantationCompleted args)
    {
        _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], true);
        _humanoidAppearanceSystem.SetBaseLayerId(args.Body, ent.Comp.Layer, ent.Comp.Prototype);
    }

    private static DamageSpecifier InvertDamage(DamageSpecifier damage)
    {
        var inverted = new DamageSpecifier(damage);
        foreach (var (key, value) in damage.DamageDict)
        {
            inverted.DamageDict[key] = -value;
        }

        return inverted;
    }
}

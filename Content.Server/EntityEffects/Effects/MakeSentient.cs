using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid; //Delta-V - Banning humanoids from becoming ghost roles.

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSentient : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        // Delta-V: Do not allow humanoids to become sentient. Intended to stop people from
        // repeatedly cloning themselves and using cognizine on their bodies.
        // HumanoidAppearanceComponent is common to all player species, and is also used for the
        // Ripley pilot whitelist, so there's a precedent for using it for this kind of check.
        if (entityManager.HasComponent<HumanoidAppearanceComponent>(uid))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}

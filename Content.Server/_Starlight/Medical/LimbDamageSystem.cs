using Content.Shared._Starlight.Medical.Damage;
using Content.Shared.Body.Components;

namespace Content.Server._Starlight.Medical;

public sealed class LimbDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BodyComponent, DamageBeforeApplyEvent>(OnDamage);
    }

    private void OnDamage(Entity<BodyComponent> ent, ref DamageBeforeApplyEvent args)
    {
        // Intentionally left empty; hook exists for future limb damage behavior.
    }
}

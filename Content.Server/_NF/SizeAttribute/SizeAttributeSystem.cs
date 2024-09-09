using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Content.Shared._NF.SizeAttribute;
using Content.Shared.Item.PseudoItem;

namespace Content.Server.SizeAttribute
{
    public sealed class SizeAttributeSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SizeAttributeComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, SizeAttributeComponent component, ComponentInit args)
        {
            if (component.Tall && TryComp<TallWhitelistComponent>(uid, out var tall))
            {
                Scale(uid, component, tall.Scale, tall.Density, tall.CosmeticOnly);
                PseudoItem(uid, component, tall.PseudoItem);
            }
            else if (component.Short && TryComp<ShortWhitelistComponent>(uid, out var smol))
            {
                Scale(uid, component, smol.Scale, smol.Density, smol.CosmeticOnly);
                PseudoItem(uid, component, smol.PseudoItem);
            }
        }

        private void PseudoItem(EntityUid uid, SizeAttributeComponent component, bool active)
        {
            if (active)
            {
                if (TryComp<PseudoItemComponent>(uid, out var pseudoI))
                    return;

                _entityManager.AddComponent<PseudoItemComponent>(uid);
            }
            else
            {
                if (!TryComp<PseudoItemComponent>(uid, out var pseudoI))
                    return;

                _entityManager.RemoveComponent<PseudoItemComponent>(uid);
            }
        }

        private void Scale(EntityUid uid, SizeAttributeComponent component, float scale, float density, bool cosmeticOnly)
        {
            if (scale <= 0f && density <= 0f)
                return;

            _entityManager.EnsureComponent<ScaleVisualsComponent>(uid);

            var appearanceComponent = _entityManager.EnsureComponent<AppearanceComponent>(uid);
            if (!_appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
                oldScale = Vector2.One;

            _appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

            if (!cosmeticOnly && _entityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                foreach (var (id, fixture) in manager.Fixtures)
                {
                    if (!fixture.Hard || fixture.Density <= 1f)
                        continue; // This will skip the flammable fixture and any other fixture that is not supposed to contribute to mass

                    switch (fixture.Shape)
                    {
                        case PhysShapeCircle circle:
                            _physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    _physics.SetDensity(uid, id, fixture, density);
                }
            }
        }
    }

    [ByRefEvent]
    public readonly record struct ScaleEntityEvent(EntityUid Uid) { }
}

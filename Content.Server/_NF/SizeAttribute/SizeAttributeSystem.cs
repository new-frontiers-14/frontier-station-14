using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Content.Shared._NF.SizeAttribute;
using Content.Shared.Nyanotrasen.Item.PseudoItem;

namespace Content.Server.SizeAttribute
{
    public sealed class SizeAttributeSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SizeAttributeComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, SizeAttributeComponent component, ComponentInit args)
        {
            if (component.Tall && TryComp<TallWhitelistComponent>(uid, out var tallComp))
            {
                Scale(uid, component, tallComp.Scale, tallComp.Density, tallComp.CosmeticOnly);
                PseudoItem(uid, component, tallComp.PseudoItem, tallComp.Shape, tallComp.StoredOffset, tallComp.StoredRotation);
            }
            else if (component.Short && TryComp<ShortWhitelistComponent>(uid, out var shortComp))
            {
                Scale(uid, component, shortComp.Scale, shortComp.Density, shortComp.CosmeticOnly);
                PseudoItem(uid, component, shortComp.PseudoItem, shortComp.Shape, shortComp.StoredOffset, shortComp.StoredRotation);
            }
        }

        private void PseudoItem(EntityUid uid, SizeAttributeComponent _, bool active, List<Box2i>? shape, Vector2i? storedOffset, float storedRotation)
        {
            if (active)
            {
                var pseudoI = _entityManager.EnsureComponent<PseudoItemComponent>(uid);

                pseudoI.StoredRotation = storedRotation;
                pseudoI.StoredOffset = storedOffset ?? new(0, 17);
                pseudoI.Shape = shape ?? new List<Box2i>
                {
                    new Box2i(0, 0, 1, 4),
                    new Box2i(0, 2, 3, 4),
                    new Box2i(4, 0, 5, 4)
                };
            }
            else
            {
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

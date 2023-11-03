using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

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
            if (!TryComp<SizeAttributeWhitelistComponent>(uid, out var whitelist))
                return;

            var scale = 0f;
            var density = 0f;
            if (whitelist.Tall)
            {
                scale = whitelist.TallScale;
                density = whitelist.TallDensity;
            }
            else if (whitelist.Short)
            {
                scale = whitelist.ShortScale;
                density = whitelist.ShortDensity;
            }

            if (scale <= 0f && density <= 0f)
                return;

            Scale(uid, component, scale, density);
        }

        private void Scale(EntityUid uid, SizeAttributeComponent component, float scale, float density)
        {
            _entityManager.EnsureComponent<ScaleVisualsComponent>(uid);

            var appearanceComponent = _entityManager.EnsureComponent<AppearanceComponent>(uid);
            if (!_appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
                oldScale = Vector2.One;

            _appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

            if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                foreach (var (id, fixture) in manager.Fixtures)
                {
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

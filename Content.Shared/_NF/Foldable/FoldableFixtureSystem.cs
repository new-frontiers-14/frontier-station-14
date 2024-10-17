using Content.Shared.Foldable;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._NF.Foldable.Systems;

public sealed class FoldableFixtureSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FoldableFixtureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FoldableFixtureComponent, FoldedEvent>(OnFolded);
    }

    private void OnMapInit(EntityUid uid, FoldableFixtureComponent component, MapInitEvent args)
    {
        if (TryComp<FoldableComponent>(uid, out var foldable))
            SetFoldedFixture(uid, foldable.IsFolded, component);
    }

    private void OnFolded(EntityUid uid, FoldableFixtureComponent? component, ref FoldedEvent args)
    {
        SetFoldedFixture(uid, args.IsFolded, component);
    }

    private void SetFoldedFixture(EntityUid uid, bool isFolded, FoldableFixtureComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Fixture == null)
            return;

        var fixture = _fixtures.GetFixtureOrNull(uid, component.Fixture);

        if (!isFolded)
        {
            if (fixture != null)
                _physics.SetHard(uid, fixture, true);
        }
        else
        {
            if (fixture != null)
                _physics.SetHard(uid, fixture, false);
        }
    }
}

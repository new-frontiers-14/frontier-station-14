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
            SetFoldedFixtures(uid, foldable.IsFolded, component);
    }

    private void OnFolded(EntityUid uid, FoldableFixtureComponent? component, ref FoldedEvent args)
    {
        SetFoldedFixtures(uid, args.IsFolded, component);
    }

    // Sets all relevant fixtures for the entity to an appropriate hard/soft state.
    private void SetFoldedFixtures(EntityUid uid, bool isFolded, FoldableFixtureComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (isFolded)
        {
            SetAllFixtureHardness(uid, component.FoldedFixtures, true);
            SetAllFixtureHardness(uid, component.UnfoldedFixtures, false);
        }
        else
        {
            SetAllFixtureHardness(uid, component.FoldedFixtures, false);
            SetAllFixtureHardness(uid, component.UnfoldedFixtures, true);
        }
    }

    // Sets all fixtures on an entity in a list to either be hard or soft.
    void SetAllFixtureHardness(EntityUid uid, List<string> fixtures, bool hard)
    {
        foreach (var fixName in fixtures)
        {
            var fixture = _fixtures.GetFixtureOrNull(uid, fixName);
            if (fixture != null)
                _physics.SetHard(uid, fixture, hard);
        }
    }
}

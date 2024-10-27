using Content.Server.Kitchen.EntitySystems;
using Content.Server.Nyanotrasen.Kitchen.EntitySystems;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles fried trait, causing the affected to look crispy.
/// </summary>
public sealed class FriedTraitSystem : EntitySystem
{
    [Dependency] private readonly DeepFryerSystem _deepFryerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FriedTraitComponent, ComponentStartup>(SetupFriedTrait);
    }

    private void SetupFriedTrait(EntityUid uid, FriedTraitComponent component, ComponentStartup args)
    {
        _deepFryerSystem.MakeCrispy(uid, component.CrispinessLevelSet);
    }
}

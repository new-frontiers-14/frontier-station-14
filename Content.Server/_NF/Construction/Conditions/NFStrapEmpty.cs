using Content.Shared.Construction;
using JetBrains.Annotations;
using Content.Shared.Examine;
using Content.Shared.Buckle.Components;

namespace Content.Server.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class NFStrapEmpty : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out StrapComponent? strap))
            return true; // No strap, nothing can be buckled.

        return strap.BuckledEntities.Count == 0;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent(entity, out StrapComponent? strap)) return false;

        if (strap.BuckledEntities.Count > 0)
        {
            args.PushMarkup(Loc.GetString("construction-examine-condition-nf-strap-empty", ("entityName", entMan.GetComponent<MetaDataComponent>(entity).EntityName)) + "\n");
            return true;
        }

        return false;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-nf-strap-empty"
        };
    }
}
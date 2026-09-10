using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

public sealed partial class TraitPrototype
{
    [DataField]
    public HashSet<ProtoId<TraitPrototype>> RequiredTraits = new();

    public bool AreRequirementsMet(IReadOnlySet<ProtoId<TraitPrototype>> selectedTraits)
    {
        foreach (var requiredTrait in RequiredTraits)
        {
            if (!selectedTraits.Contains(requiredTrait))
                return false;
        }

        return true;
    }

    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesBlacklist = new();

    public bool IsSpeciesAllowed(ProtoId<SpeciesPrototype> species)
    {
        return !SpeciesBlacklist.Contains(species);
    }
}

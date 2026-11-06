using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.EntityEffects.Effect;

/// <summary>
/// Requires that the metabolizing body is a humanoid, with an optional whitelist/blacklist.
/// </summary>
public sealed partial class NFIsHumanoid : EntityEffectCondition
{
    /// <summary>
    /// The whitelist (or blacklist if inverse is true) of species to select.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>>? Whitelist = null;

    /// <summary>
    /// If true, the metabolizer's species must be in the list to process this effect.
    /// If false, the metabolizer's species cannot be in the list.
    /// </summary>
    [DataField]
    public bool Inverse;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs)
        {
            if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var humanoidAppearance))
                return false;

            if (Whitelist != null && Whitelist.Contains(humanoidAppearance.Species) != Inverse)
                return false;

            return true;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        if (Whitelist == null || Whitelist.Count == 0)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-species-type-empty");
        }
        else
        {
            var message = Inverse ? "reagent-effect-condition-guidebook-species-type-blacklist" : "reagent-effect-condition-guidebook-species-type-whitelist";
            var localizedSpecies = Whitelist.Select(p => Loc.GetString("reagent-effect-condition-guidebook-species-type-species", ("species", Loc.GetString(prototype.Index(p).Name)))).ToList();
            var list = ContentLocalizationManager.FormatListToOr(localizedSpecies);
            return Loc.GetString(message, ("species", list));
        }
    }
}

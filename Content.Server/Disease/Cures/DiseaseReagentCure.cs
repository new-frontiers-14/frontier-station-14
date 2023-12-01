using Content.Shared.Disease;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    /// Cures the disease if a certain amount of reagent
    /// is in the host's chemstream.
    /// </summary>
    public sealed partial class DiseaseReagentCure : DiseaseCure
    {
        [DataField("min")]
        public FixedPoint2 Min = 5;
        [DataField("reagent")]
        public ReagentId? Reagent;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.DiseasedEntity, out var bloodstream))
                return false;

            var quant = FixedPoint2.Zero;
            if (Reagent is ReagentId reagentToAdd && bloodstream.ChemicalSolution.ContainsReagent(reagentToAdd))
            {
                quant = bloodstream.ChemicalSolution.GetReagentQuantity(reagentToAdd);
            }
            return quant >= Min;
        }

        public override string CureText()
        {
            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
            if (Reagent is not ReagentId reagentToAdd)
                return string.Empty;
            return (Loc.GetString("diagnoser-cure-reagent", ("units", Min), ("reagent", Reagent)));
        }
    }
}

using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Server.Atmos.Miasma;
using Content.Server.Disease;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// The miasma system rotates between 1 disease at a time.
    /// This gives all entities the disease the miasme system is currently on.
    /// For things ingested by one person, you probably want ChemCauseRandomDisease instead.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemMiasmaPoolSource : ReagentEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-cause-disease");

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            string disease = EntitySystem.Get<RottingSystem>().RequestPoolDisease();

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, disease);
        }
    }
}

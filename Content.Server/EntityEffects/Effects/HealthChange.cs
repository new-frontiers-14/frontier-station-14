using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.EntityEffects.Effects
{
    /// <summary>
    /// Default metabolism used for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class HealthChange : EntityEffect
    {
        /// <summary>
        /// Damage to apply every cycle. Damage Ignores resistances.
        /// </summary>
        [DataField(required: true)]
        [JsonPropertyName("damage")]
        public DamageSpecifier Damage = default!;

        /// <summary>
        ///     Should this effect scale the damage by the amount of chemical in the solution?
        ///     Useful for touch reactions, like styptic powder or acid.
        ///     Only usable if the EntityEffectBaseArgs is an EntityEffectReagentArgs.
        /// </summary>
        [DataField]
        [JsonPropertyName("scaleByQuantity")]
        public bool ScaleByQuantity;

        [DataField]
        [JsonPropertyName("ignoreResistances")]
        public bool IgnoreResistances = true;

        protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var damages = new List<string>();
            var heals = false;
            var deals = false;

            var damageSpec = new DamageSpecifier(Damage);

            foreach (var group in prototype.EnumeratePrototypes<DamageGroupPrototype>())
            {
                if (!damageSpec.TryGetDamageInGroup(group, out var amount))
                    continue;

                var relevantTypes = damageSpec.DamageDict
                    .Where(x => x.Value != FixedPoint2.Zero && group.DamageTypes.Contains(x.Key)).ToList();

                if (relevantTypes.Count != group.DamageTypes.Count)
                    continue;

                // Frontier: explicitly check that all damage types have the same amount
                var firstValue = relevantTypes.FirstOrDefault().Value;
                bool allValuesEqual = true;
                foreach (var type in relevantTypes)
                {
                    if (FixedPoint2.Abs(type.Value - firstValue) >= 0.02) // Need to account for remainder issues
                    {
                        allValuesEqual = false;
                        break;
                    }
                }
                if (!allValuesEqual)
                    continue;
                // End Frontier

                var sign = FixedPoint2.Sign(amount);

                if (sign < 0)
                    heals = true;
                if (sign > 0)
                    deals = true;

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", group.LocalizedName),
                        ("amount", MathF.Abs(amount.Float())),
                        ("deltasign", sign)
                    ));

                foreach (var type in group.DamageTypes)
                {
                    damageSpec.DamageDict.Remove(type);
                }
            }

            foreach (var (kind, amount) in damageSpec.DamageDict)
            {
                var sign = FixedPoint2.Sign(amount);

                if (sign < 0)
                    heals = true;
                if (sign > 0)
                    deals = true;

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                        ("amount", MathF.Abs(amount.Float())),
                        ("deltasign", sign)
                    ));
            }

            var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

            return Loc.GetString("reagent-effect-guidebook-health-change",
                ("chance", Probability),
                ("changes", ContentLocalizationManager.FormatList(damages)),
                ("healsordeals", healsordeals));
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            var scale = FixedPoint2.New(1);

            if (args is EntityEffectReagentArgs reagentArgs)
            {
                scale = ScaleByQuantity ? reagentArgs.Quantity * reagentArgs.Scale : reagentArgs.Scale;
            }

            args.EntityManager.System<DamageableSystem>().TryChangeDamage(
                args.TargetEntity,
                Damage * scale,
                IgnoreResistances,
                interruptsDoAfters: false);
        }
    }
}

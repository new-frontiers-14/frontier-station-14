using System.Text.Json.Serialization;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReactionEffects
{
    [DataDefinition]
    public sealed partial class ExplosionReactionEffect : ReagentEffect
    {
        /// <summary>
        ///     The type of explosion. Determines damage types and tile break chance scaling.
        /// </summary>
        [DataField("explosionType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ExplosionPrototype>))]
        [JsonIgnore]
        public string ExplosionType = default!;

        /// <summary>
        ///     The max intensity the explosion can have at a given tile. Places an upper limit of damage and tile break
        ///     chance.
        /// </summary>
        [DataField("maxIntensity")]
        [JsonIgnore]
        public float MaxIntensity = 5;

        /// <summary>
        ///     How quickly intensity drops off as you move away from the epicenter
        /// </summary>
        [DataField("intensitySlope")]
        [JsonIgnore]
        public float IntensitySlope = 1;

        /// <summary>
        ///     The maximum total intensity that this chemical reaction can achieve. Basically here to prevent people
        ///     from creating a nuke by collecting enough potassium and water.
        /// </summary>
        /// <remarks>
        ///     A slope of 1 and MaxTotalIntensity of 100 corresponds to a radius of around 4.5 tiles.
        /// </remarks>
        [DataField("maxTotalIntensity")]
        [JsonIgnore]
        public float MaxTotalIntensity = 100;

        /// <summary>
        ///     The intensity of the explosion per unit reaction.
        /// </summary>
        [DataField("intensityPerUnit")]
        [JsonIgnore]
        public float IntensityPerUnit = 1;

        public override bool ShouldLog => true;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-explosion-reaction-effect", ("chance", Probability));
        public override LogImpact LogImpact => LogImpact.High;

        public override void Effect(ReagentEffectArgs args)
        {
            var intensity = MathF.Min((float) args.Quantity * IntensityPerUnit, MaxTotalIntensity);

            EntitySystem.Get<ExplosionSystem>().QueueExplosion(
                args.SolutionEntity,
                ExplosionType,
                intensity,
                IntensitySlope,
                MaxIntensity);
        }
    }
}

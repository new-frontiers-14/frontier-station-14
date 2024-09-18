// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Components;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;
using Content.Shared.Chemistry;

namespace Content.Server.Nutrition.EntitySystems;

// Frontier: extending food system to handle species-specific digestion quirks.
public sealed partial class FoodSystem : EntitySystem // Frontier: sealed<partial
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public struct DigestionResult
    {
        public bool ShowFlavors;
    }

    DigestionResult DigestFood(Entity<FoodComponent> food, Entity<StomachComponent> stomach, Solution eatenSolution, EntityUid target, EntityUid _)
    {
        var result = new DigestionResult
        {
            ShowFlavors = true
        };

        // Frontier - Food quality system
        if (!_prototype.TryIndex(stomach.Comp.Digestion, out var digestion))
        {
            return result;
        }

        var eatenVolume = eatenSolution.Volume;
        while (digestion != null)
        {
            // Iterate through effects
            foreach (var effect in digestion.Effects)
            {
                if (_whitelistSystem.IsWhitelistFail(effect.Whitelist, food.Owner))
                    continue;

                // Run reagent conversions
                foreach (var (beforeReagent, afterDict) in effect.Conversions)
                {
                    var removedAmount = eatenSolution.RemoveReagent(new ReagentQuantity(beforeReagent, eatenVolume));
                    foreach (var (afterReagent, afterRatio) in afterDict)
                    {
                        eatenSolution.AddReagent(new ReagentQuantity(afterReagent, removedAmount * afterRatio));
                    }
                }

                var args = new EntityEffectReagentArgs(target, EntityManager, stomach.Owner, eatenSolution, eatenSolution.Volume, null, ReactionMethod.Ingestion, 1.0f);
                foreach (var entEffect in effect.Effects)
                {
                    if (!EntityEffectExt.ShouldApply(entEffect, args, _random))
                        continue;
                    entEffect.Effect(args);
                }
            }

            // Get next digestion to run
            if (!_prototype.TryIndex(stomach.Comp.Digestion, out digestion))
            {
                digestion = null;
            }
        }

        return result;
    }
}

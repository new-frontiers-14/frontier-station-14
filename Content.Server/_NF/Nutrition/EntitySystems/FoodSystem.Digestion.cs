// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Server.Medical;
using Content.Server.Nutrition.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Content.Shared.Jittering;
using Content.Server.Chat.Systems;
using Content.Shared.Tag;
using Content.Shared.Nutrition;
using Content.Server.Body.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Nutrition.EntitySystems;

// Frontier: extending food system to handle species-specific digestion quirks.
public partial class FoodSystem : EntitySystem // Frontier: sealed<partial
{
    [Dependency] private readonly VomitSystem _vomit = default!; // Frontier
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!; // Frontier
    [Dependency] protected readonly IRobustRandom RobustRandom = default!; // Frontier
    [Dependency] private readonly SharedJitteringSystem _jittering = default!; // Frontier
    [Dependency] private readonly ChatSystem _chat = default!; // Frontier
    [Dependency] private readonly TagSystem _tag = default!; // Frontier

    protected struct DigestionResult
    {
        public bool ShowFlavors;
    }

    DigestionResult DigestFood(Entity<FoodComponent> entity, StomachComponent stomach, FixedPoint2 foodAmount, ref ConsumeDoAfterEvent args)
    {
        /// Frontier - Food quality system
        switch (stomach.DigestionFunc)
        {
            case DigestionFunction.Normal:
            default:
                break;
            case DigestionFunction.Goblin:
                return GoblinDigestion(entity, stomach, foodAmount, ref args);
        }
        return new DigestionResult
        {
            ShowFlavors = true
        };
    }

    DigestionResult GoblinDigestion(Entity<FoodComponent> entity, StomachComponent stomach, FixedPoint2 foodAmount, ref ConsumeDoAfterEvent args)
    {
        DigestionResult result;
        result.ShowFlavors = true;
        var foodQuality = entity.Comp.Quality;
        // TODO: Add detection for fried food on nasty to update it to toxin for goblins.
        // TODO: Add inspect food but only for goblin eyes to see, goblins can tell food quality.

        string[] toxinsRegent = { "Toxin", "CarpoToxin", "Mold", "Amatoxin", "SulfuricAcid", "Bungotoxin" };

        EntityUid eater = args?.Target ?? EntityUid.Invalid;
        TryComp<BloodstreamComponent>(eater, out var bloodStream);

        string? print = null;
        float jitterStrength = 0.0f;
        bool stutter = false;
        bool emote = false;
        bool vomit = false;
        int speedDivisor = 0;
        int damageDivisor = 0;

        // Assign parameters based on food quality
        if (_tag.HasAnyTag(entity, "Mail", "Trash", "Fiber"))
        {
            speedDivisor = 1;
        }
        else
            switch (foodQuality)
            {
                case FoodQuality.High:
                    result.ShowFlavors = false;
                    print = Loc.GetString("food-system-toxin");
                    stutter = true;
                    jitterStrength = 8.0f;
                    vomit = true;
                    damageDivisor = 3;
                    emote = true;
                    break;
                case FoodQuality.Normal:
                    result.ShowFlavors = false;
                    print = Loc.GetString("food-system-nasty");
                    stutter = true;
                    jitterStrength = 4.0f;
                    damageDivisor = 5;
                    break;
                case FoodQuality.Junk:
                    speedDivisor = 7;
                    break;
                case FoodQuality.Nasty:
                    speedDivisor = 5;
                    break;
                case FoodQuality.Toxin:
                    speedDivisor = 3;
                    break;
            }

        // Run goblin food behaviour
        if (_solutionContainer.ResolveSolution(eater, stomach.BodySolutionName, ref stomach.Solution))
        {
            foreach (var reagent in toxinsRegent)
                _solutionContainer.RemoveReagent(stomach.Solution.Value, reagent, FixedPoint2.New((int) foodAmount)); // Remove from body before it goes to blood
            _solutionContainer.RemoveReagent(stomach.Solution.Value, "Flavorol", FixedPoint2.New((int) foodAmount)); // Remove from body before it goes to blood
        }
        if ((speedDivisor > 0 || damageDivisor > 0) &&
            _solutionContainer.ResolveSolution(eater, bloodStream!.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
        {
            if (speedDivisor > 0)
                _solutionContainer.TryAddReagent(bloodStream.ChemicalSolution.Value, "Stimulants", FixedPoint2.New((int) foodAmount / speedDivisor), out _); // Add to blood
            if (damageDivisor > 0)
                _solutionContainer.TryAddReagent(bloodStream.ChemicalSolution.Value, "Toxin", FixedPoint2.New((int) foodAmount / damageDivisor), out _); // Add to blood
        }
        if (stutter)
            _stuttering.DoStutter(eater, TimeSpan.FromSeconds(5), false); // Gives stuttering
        if (jitterStrength > 0.0f)
            _jittering.DoJitter(eater, TimeSpan.FromSeconds(5), true, jitterStrength * 10f, jitterStrength, true, null);
        if (emote)
            _chat.TryEmoteWithoutChat(eater, "Laugh");

        if (vomit && RobustRandom.Prob(.05f)) // 5% to puke
            _vomit.Vomit(eater);

        return result;
    }

}
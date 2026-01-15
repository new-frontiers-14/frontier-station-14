using Content.Shared.CollectiveMind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Shared.GameTicking;
using Robust.Shared.Utility;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;
using System.Text;
using Content.Shared.Stunnable;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Bed.Sleep;

namespace Content.Server.CollectiveMind;

public sealed partial class CollectiveMind : SharedCollectiveMindSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        // garbling
        SubscribeLocalEvent<CollectiveMindComponent, CollectiveMindMessageAttemptEvent>(OnCollectiveMindMessage, after: [typeof(ReplacementAccentSystem)]);
    }

    private void OnCollectiveMindMessage(Entity<CollectiveMindComponent> ent, ref CollectiveMindMessageAttemptEvent args)
    {
        var uid = ent.Owner;

        // we need to check if the entity is sleeping, or crit
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            if (mobState.CurrentState == MobState.Critical || TryComp<SleepingComponent>(uid, out _))
            {
                if (ent.Comp.CorruptWhenUnconscious)
                    args.Message = Corrupt(args.Message, ref ent.Comp);
                if (ent.Comp.BlockWhenUnconscious)
                    args.Cancel();
            }
        }
    }

    private string Corrupt(string message, ref CollectiveMindComponent comp)
    {
        var outMsg = new StringBuilder();
        // Linear interpolation of character damage probability
        foreach (var letter in message)
        {
            if (_random.Prob(comp.CorruptionChanceWhenUnconscious)) // Corrupt!
            {
                outMsg.Append(CorruptLetter(letter));
            }
            else // Safe!
            {
                outMsg.Append(letter);
            }
        }
        return outMsg.ToString();
    }

    private string CorruptLetter(char letter)
    {
        var res = _random.NextDouble();
        return res switch
        {
            < 0.0 => letter.ToString(), // shouldn't be less than 0!
            < 0.1 => CorruptRandom(), // 15% chance to replace with one random character
            < 0.25 => CorruptRandomMultiple(_random.Next(2, 5)), // 10% chance for between 2 and 5 random characters
            < 0.5 => "", // 25% chance to remove character
            < 0.75 => CorruptRepeat(letter), // 25% to repeat the character
            < 0.9 => CorruptRepeat(CorruptRandom()[0]), // 15% to repeat a corrupted character
            < 1.0 => CorruptRepeat(CorruptRandomMultiple(_random.Next(2, 5))[0]), // 10% chance for between 2 and 5 random corrupted characters
            _ => letter.ToString(), // shouldn't be greater than 1!
        };
    }

    private string CorruptRandom()
    {
        const string ran = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return ran[_random.NextByte((byte)ran.Length)].ToString();
    }

    private string CorruptRandomMultiple(int repeats)
    {
        string corrupted = "";
        for (int repeat = 0; repeat < repeats; repeat++)
        {
            const string ran = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            corrupted += ran[_random.NextByte((byte)ran.Length)].ToString();
        }
        return corrupted;
    }

    private string CorruptRepeat(char letter)
    {
        // 25% chance to add another character in the streak
        // (kind of like "exploding dice")
        // Solved numerically in closed form for streaks of bernoulli variables with p = 0.25
        var numRepeats = _random.NextDouble() switch
        {
            < 0.75000000 => 2,
            < 0.93750000 => 3,
            < 0.98437500 => 4,
            < 0.99609375 => 5,
            < 0.99902344 => 6,
            < 0.99975586 => 7,
            < 0.99993896 => 8,
            < 0.99998474 => 9,
            _ => 10,
        };
        return new string(letter, numRepeats);
    }
}

using System.Text;
using System.Text.RegularExpressions;
using Content.Server._NF.Speech.Components;
using Content.Shared.Speech;
using Linguini.Shared.Util;
using Robust.Shared.Random;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class SlothfulAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlothfulAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private void OnAccentGet(Entity<SlothfulAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }

    private static readonly Regex Vowels = new("([aeiouy])", RegexOptions.IgnoreCase);
    private static readonly Regex WhiteSpace = new("\\s");

    /// <summary>
    /// Matches the end of the string only if the last character is a "word" character.
    /// </summary>
    private static readonly Regex NoFinalPunctuation = new("\\w\\.\\z");

    public string Accentuate(string message)
    {
        var length = message.Length;

        var finalMessage = new StringBuilder();

        string newLetter;

        for (var i = 0; i < length; i++)
        {
            newLetter = message[i].ToString();

            // Ignore the word 'I' or contractions using it
            if (newLetter == "I")
            {
                if ((i != 0 && message[i-1].ToString() == " ") && (i != message.Length - 1 && (message[i+1].ToString() == " " || message[i+1].ToString() == "'")))
                {
                    finalMessage.Append(newLetter);
                    continue;
                }
            }

            // Ignore the first capital in a sentence unless followed by another capital
            if (message[i].IsAsciiUppercase() && (i != message.Length - 1 && !message[i+1].IsAsciiUppercase()))
            {
                finalMessage.Append(newLetter);
                continue;
            }

            // If it's a vowel, random chance to repeat it with variance
            if (Vowels.IsMatch(newLetter) && _random.Prob(0.6f))
            {
                // Low chance to quadruple, less low chance to double, otherwise triple the vowel
                if (_random.Prob(0.05f))
                {
                    newLetter = $"{newLetter}{newLetter}{newLetter}{newLetter}";
                }
                else if (_random.Prob(0.1f))
                {
                    newLetter = $"{newLetter}{newLetter}";
                }
                else
                {
                    newLetter = $"{newLetter}{newLetter}{newLetter}";
                }
            }

            // If it's whitespace, random chance to replace with ellipsis...
            if (WhiteSpace.IsMatch(newLetter) && _random.Prob((0.15f)))
            {
                newLetter = "";
                if (i != 0 && message[i-1] != '.')
                {
                    newLetter = ".";
                }
                if (_random.Prob(0.75f))
                {
                    newLetter += $".. ";
                }
                else
                {
                    newLetter += $". ";
                }
            }
            finalMessage.Append(newLetter);
        }

        message = finalMessage.ToString();

        // Add "..." to the end, if the last character is part of a word...
        if (NoFinalPunctuation.IsMatch(message))
            message += "..";

        return message;
    }
}

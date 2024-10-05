using Content.Server._NF.Speech.Components;
using Robust.Shared.Random;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Linq;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class CavemanAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CavemanAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private string Convert(string message, CavemanAccentComponent component)
    {
        string msg = _replacement.ApplyReplacements(message, "caveman");

        string[] words = msg.Split(' ');
        List<string> modifiedWords = new List<string>();

        foreach (var word in words)
        {
            string endPunctuation = "";
            int actualLength = word.Length;

            for (int letterIndex = word.Length - 1; letterIndex >= 0; letterIndex--)
            {
                if (word[letterIndex] != '-' && char.IsPunctuation(word[letterIndex]))
                {
                    endPunctuation = word[letterIndex] + endPunctuation;
                    actualLength = letterIndex; // Length of word = index of first punctuation
                }
                else
                {
                    break;
                }
            }

            var modifiedWord = word;

            if (actualLength > CavemanAccentComponent.MaxWordLength)
            {
                modifiedWord = GetGrunt();
                modifiedWord += endPunctuation;

                modifiedWords.Add(modifiedWord);

                continue;
            }

            modifiedWord = TryRemovePunctuation(modifiedWord);

            modifiedWord = TryConvertNumbers(modifiedWord);

            // Weird combinations of numbers/punctuations and letters
            foreach (var c in modifiedWord)
            {
                if (!char.IsLetter(c) && c != '-')
                {
                    modifiedWord = GetGrunt();
                    break;
                }
            }

            modifiedWord += endPunctuation;

            modifiedWords.Add(modifiedWord);
        }

        if (modifiedWords.Count == 0)
        {
            modifiedWords.Append(GetGrunt());
        }

        return string.Join(' ', modifiedWords);
    }

    private void OnAccentGet(EntityUid uid, CavemanAccentComponent component, AccentGetEvent args)
    {
        args.Message = Convert(args.Message, component);
    }

    private string GetGrunt()
    {
        var grunt = Loc.GetString(_random.Pick(CavemanAccentComponent.Grunts));

        if (_random.Prob(0.5f))
        {
            grunt += "-";
            grunt += Loc.GetString(_random.Pick(CavemanAccentComponent.Grunts));
        }
        return grunt;
    }

    private string TryRemovePunctuation(string word)
    {
        return word.Trim(['.', ',', '!', '?', ';', ':']);
    }

    private string TryConvertNumbers(string word)
    {
        int num;

        if (int.TryParse(word, out num))
        {
            num = int.Max(0, num); //Negatives treated as zero.
            if (num < CavemanAccentComponent.Numbers.Count)
            {
                return Loc.GetString(CavemanAccentComponent.Numbers[num]);
            }
            else
            {
                return Loc.GetString(CavemanAccentComponent.LargeNumberString);
            }
        }

        return word;
    }

}

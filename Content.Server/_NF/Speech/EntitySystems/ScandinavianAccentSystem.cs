using System.Text;
using Robust.Shared.Random;
using Content.Server.Speech.EntitySystems;
using Content.Server._NF.Speech.Components;
using Content.Server.Speech;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class ScandinavianAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly IReadOnlyDictionary<char, char[]> Vowels = new Dictionary<char, char[]>()
    {
        { 'A',  ['Å','Ä','Æ'] },
        { 'a',  ['å','ä','æ'] },
        { 'O',  ['Ö','Ø'] },
        { 'o',  ['ö','ø'] },
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<ScandinavianAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Apply word replacements
        var msg = _replacement.ApplyReplacements(message, "scandinavian");

        var msgBuilder = new StringBuilder(msg);
        var umlautCooldown = 0;

        for (var i = 0; i < msgBuilder.Length; i++)
        {
            var tempChar = msgBuilder[i];

            // Replace specific consonants
            msgBuilder[i] = tempChar switch
            {
                'W' => 'V',
                'w' => 'v',
                'J' => 'Y',
                'j' => 'y',
                _ => msgBuilder[i]
            };

            // Umlaut logic: avoid clusters
            if (umlautCooldown == 0 && Vowels.TryGetValue(tempChar, out var replacements))
            {
                if (_random.Prob(0.1f)) // 10% of all eligible vowels become umlauts)
                {
                    msgBuilder[i] = _random.Pick(replacements);
                    umlautCooldown = 4; // Prevents consecutive umlauts
                }
            }
            else if (umlautCooldown > 0)
            {
                umlautCooldown--;
            }
        }

        return msgBuilder.ToString();
    }

    private void OnAccent(Entity<ScandinavianAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}

using System.Linq; // Start Frontier: Make BarkAccent growly instead of racist sounding.
using System.Text; // Start Frontier: Make BarkAccent growly instead of racist sounding.
using System.Text.RegularExpressions; // Frontier: Make BarkAccent growly instead of racist sounding.
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class BarkAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Barks = new List<string>{
            " Woof!", " WOOF", " wof-wof"
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "ah", "arf" },
            { "Ah", "Arf" },
            { "oh", "oof" },
            { "Oh", "Oof" },
        };

        private static readonly Regex BarkRegex = new("!"); // Frontier: Make BarkAccent growly instead of racist sounding.


        public override void Initialize()
        {
            SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            // Start Frontier: Make BarkAccent growly instead of racist sounding.
            var punctuation = message.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = message.Split().Select(x => x.Trim(punctuation));

            // Add a number of repeats for the letter r at the end of a word, minimum 1 and maximum 4.
            foreach (var word in words.Distinct())
            {
                if (!word.EndsWith("r", StringComparison.OrdinalIgnoreCase))
                    continue;
                var repeats = _random.Next(100);
                var extraRs = 1 + repeats / 60 + repeats / 80 + repeats / 90;
                var repeatR = word.Equals(word.ToUpperInvariant()) ? "R" : "r";
                message = message.Replace(word, word + new string(repeatR[^1], extraRs));
            }

            return BarkRegex.Replace(message,
                _ => _random.Pick(Barks));
            // End Frontier: Make BarkAccent growly instead of racist sounding.
        }

        private void OnAccent(EntityUid uid, BarkAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}

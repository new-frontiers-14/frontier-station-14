using System.Text.RegularExpressions;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server._Park.Speech.EntitySystems
{
    public sealed class ShadowkinAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly Regex mRegex = new(@"[adgjmpsvy]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex aRegex = new(@"[behknqtwz]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rRegex = new(@"[cfilorux]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void Initialize()
        {
            SubscribeLocalEvent<ShadowkinAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            message = message.Trim();

            // Replace letters with other letters
            message = mRegex.Replace(message, "m");
            message = aRegex.Replace(message, "a");
            message = rRegex.Replace(message, "r");

            return message;
        }

        private void OnAccent(EntityUid uid, ShadowkinAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.Managers;

public sealed class ChatSanitizationManager : IChatSanitizationManager
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private static readonly Dictionary<string, string> SmileyToEmote = new()
    {
        // I could've done this with regex, but felt it wasn't the right idea.
        { ":)", "chatsan-smiles" },
        { ":]", "chatsan-smiles" },
        { "=)", "chatsan-smiles" },
        { "=]", "chatsan-smiles" },
        { "(:", "chatsan-smiles" },
        { "[:", "chatsan-smiles" },
        { "(=", "chatsan-smiles" },
        { "[=", "chatsan-smiles" },
        { "^^", "chatsan-smiles" },
        { "^-^", "chatsan-smiles" },
        { ":(", "chatsan-frowns" },
        { ":[", "chatsan-frowns" },
        { "=(", "chatsan-frowns" },
        { "=[", "chatsan-frowns" },
        { "):", "chatsan-frowns" },
        { ")=", "chatsan-frowns" },
        { "]:", "chatsan-frowns" },
        { "]=", "chatsan-frowns" },
        { ":D.", "chatsan-smiles-widely" }, // Frontier: add period
        { "D:", "chatsan-frowns-deeply" },
        { ":O.", "chatsan-surprised" }, // Frontier: add period
        { ":3", "chatsan-smiles" }, //nope
        { ":S.", "chatsan-uncertain" }, // Frontier: add period
        { ":>", "chatsan-grins" },
        { ":<", "chatsan-pouts" },
        { "xD.", "chatsan-laughs" }, // Frontier: add period
        { ":'(", "chatsan-cries" },
        { ":'[", "chatsan-cries" },
        { "='(", "chatsan-cries" },
        { "='[", "chatsan-cries" },
        { ")':", "chatsan-cries" },
        { "]':", "chatsan-cries" },
        { ")'=", "chatsan-cries" },
        { "]'=", "chatsan-cries" },
        { ";-;", "chatsan-cries" },
        { ";_;", "chatsan-cries" },
        { "qwq.", "chatsan-cries" }, // Frontier: add period
        { ":u.", "chatsan-smiles-smugly" }, // Frontier: add period
        { ":v.", "chatsan-smiles-smugly" }, // Frontier: add period
        { ">:i.", "chatsan-annoyed" }, // Frontier: add period
        { ":i.", "chatsan-sighs" }, // Frontier: add period
        { ":|", "chatsan-sighs" },
        { ":p.", "chatsan-stick-out-tongue" }, // Frontier: add period
        { ";p.", "chatsan-stick-out-tongue" }, // Frontier: add period
        { ":b.", "chatsan-stick-out-tongue" }, // Frontier: add period
        { "0-0", "chatsan-wide-eyed" },
        { "o-o.", "chatsan-wide-eyed" }, // Frontier: add period
        { "o.o.", "chatsan-wide-eyed" }, // Frontier: add period
        { "._.", "chatsan-surprised" },
        { ".-.", "chatsan-confused" },
        { "-_-", "chatsan-unimpressed" },
        { "smh.", "chatsan-unimpressed" }, // Frontier: add period
        { "o/", "chatsan-waves" },
        { "^^/", "chatsan-waves" },
        { ":/", "chatsan-uncertain" },
        { ":\\", "chatsan-uncertain" },
        { "lmao.", "chatsan-laughs" }, // Frontier: add period
        { "lmfao.", "chatsan-laughs" }, // Frontier: add period
        { "lol.", "chatsan-laughs" }, // Frontier: add period
        { "lel.", "chatsan-laughs" }, // Frontier: add period
        { "kek.", "chatsan-laughs" }, // Frontier: add period
        { "rofl.", "chatsan-laughs" }, // Frontier: add period
        { "o7", "chatsan-salutes" },
        { ";_;7", "chatsan-tearfully-salutes"},
        { "idk.", "chatsan-shrugs" }, // Frontier: add period
        { ";)", "chatsan-winks" },
        { ";]", "chatsan-winks" },
        { "(;", "chatsan-winks" },
        { "[;", "chatsan-winks" },
        { ":')", "chatsan-tearfully-smiles" },
        { ":']", "chatsan-tearfully-smiles" },
        { "=')", "chatsan-tearfully-smiles" },
        { "=']", "chatsan-tearfully-smiles" },
        { "(':", "chatsan-tearfully-smiles" },
        { "[':", "chatsan-tearfully-smiles" },
        { "('=", "chatsan-tearfully-smiles" },
        { "['=", "chatsan-tearfully-smiles" },
    };

    private bool _doSanitize;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ChatSanitizerEnabled, x => _doSanitize = x, true);
    }

    public bool TrySanitizeOutSmilies(string input, EntityUid speaker, out string sanitized, [NotNullWhen(true)] out string? emote)
    {
        if (!_doSanitize)
        {
            sanitized = input;
            emote = null;
            return false;
        }

        input = input.TrimEnd();

        foreach (var (smiley, replacement) in SmileyToEmote)
        {
            if (input.EndsWith(smiley, true, CultureInfo.InvariantCulture))
            {
                sanitized = input.Remove(input.Length - smiley.Length).TrimEnd();
                emote = Loc.GetString(replacement, ("ent", speaker));
                return true;
            }
        }

        sanitized = input;
        emote = null;
        return false;
    }
}

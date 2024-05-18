using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Corvax.Language.Components;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Corvax.Language.EntitySystems;

public sealed partial class LanguageSystem : EntitySystem
{
    [GeneratedRegex(@"\b\w+\b|\W+")]
    private static partial Regex GetWordRegex();

    [GeneratedRegex(@"\w")]
    private static partial Regex GetLetterRegex();

    private static readonly Dictionary<string, string[]> Syllables = new() {
        {"vulpkanin", ["рур", "йа", "цен", "раур", "бар", "кук", "тек", "кват", "ук", "ву", "вух", "тах", "тч", "счз", "ауч", "ист", "айн",
            "енщ", "звичз", "тут", "мир", "во", "бис", "ес", "вор", "ник", "гро", "ллл", "енем", "зандт", "тзч", "ноч", "хел", "ишт", "фар", "ва", "барам", "ийренг",
            "теч", "лач", "сам", "мак", "лич", "ген", "ор", "аг", "ецк", "гец", "стаг", "онн", "бин", "кет", "жарл", "вулф", "ейнеч", "црестхц", "азунайн", "гхзтх"]},
        {"reptilian", ["за", "аз", "зе", "ез", "зи", "из", "зо", "оз", "зу", "уз", "зс", "сз", "ха", "ах", "хе", "ех", "хи", "их", "хо", "ох", "ху", "ух", "хс", "сх",
            "ла", "ал", "ле", "ел", "ли", "ил", "ло", "ол", "лу", "ул", "лс", "сл", "ка", "ак", "ке", "ек", "ки", "ик", "ко", "ок", "ку", "ук", "кс", "ск", "са", "ас",
            "се", "ес", "си", "ис", "со", "ос", "су", "ус", "сс", "сс", "ра", "ар", "ре", "ер", "ри", "ир", "ро", "ор", "ру", "ур", "рс", "ср", "а", "а", "е", "е", "и",
            "и", "о", "о", "у", "у", "с", "с"]}
    };

    [Dependency] private readonly LanguageTranslatorSystem _translator = default!;

    private readonly Dictionary<string, Dictionary<string, string>> _dictionaries = [];

    private readonly SearchValues<char> _vowels = SearchValues.Create("ауоиэыяюеё");

    private readonly Random _random = new();

    public string? GetLanguage(EntityUid entity)
    {
        return EntityManager.TryGetComponent<LanguageSpeakerComponent>(entity, out var component) ? component.Language : null;
    }

    public static Color? GetLanguageColor(LanguageMessage message)
    {
        return message.Language switch
        {
            "vulpkanin" => Color.FromHex("#B97A57"),
            "reptilian" => Color.FromHex("#228B22"),
            _ => null
        };
    }

    public bool IsUnderstandLanguage(ICommonSession listener, LanguageMessage message)
    {
        if (message.Language is null)
            return true;

        if (!EntityManager.TryGetComponent<MindComponent>(listener.GetMind(), out var mind) || mind.CurrentEntity is null)
            return false;

        return IsUnderstandLanguage(mind.CurrentEntity.Value, message);
    }

    public bool IsUnderstandLanguage(EntityUid listener, LanguageMessage message)
    {
        if (message.Language is null)
            return true;

        if (EntityManager.TryGetComponent<LanguageSpeakerComponent>(listener, out var component) && component.Language == message.Language)
            return true;

        if (EntityManager.HasComponent<LanguageUnderstandComponent>(listener))
            return true;

        return false;
    }

    public LanguageMessage GetLanguageMessage(EntityUid entity, string message, string? language, string? transformedMessage = null)
    {
        transformedMessage ??= message;

        if (language is null || _translator.TryUseTranslator(entity, message))
            return transformedMessage;

        var words = GetWordRegex().Split(message);

        StringBuilder messageBuilder = new();

        StringBuilder wordBuilder = new();

        var dictionary = _dictionaries.GetOrNew(language);

        var syllables = Syllables[language];

        foreach (var match in GetWordRegex().Matches(message).Cast<Match>())
        {
            var word = match.Value;

            if (!GetLetterRegex().IsMatch([word[0]]))
            {
                messageBuilder.Append(word);
                continue;
            }

            var lowerWord = word.ToLower();

            ref var languageWord = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, lowerWord, out var exists);

            if (!exists)
            {
                var syllablesCount = word.Count(letter => _vowels.Contains(char.ToLower(letter)));

                syllablesCount = syllablesCount switch
                {
                    <= 1 => 2,
                    _ => _random.Next(3, 5)
                };

                for (var i = 0; i < syllablesCount; i++)
                    wordBuilder.Append(syllables[_random.Next(syllables.Length)]);

                languageWord = wordBuilder.ToString();

                wordBuilder.Clear();
            }

            if (IsUpper(word))
                languageWord = languageWord!.ToUpper();
            else if (char.IsUpper(word[0]))
                languageWord = char.ToUpper(languageWord![0]).ToString() + languageWord[1..];

            messageBuilder.Append(languageWord);
        }

        return new(transformedMessage, language, messageBuilder.ToString());
    }

    private static bool IsUpper(string str)
    {
        foreach (var c in str)
            if (char.IsLower(c))
                return false;

        return true;
    }
}

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.PowerCell;
using Content.Server.VulpLangauge;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Storage;
using Robust.Shared.Player;

namespace Content.Server.Corvax.Language;

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

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _power = default!;

    private readonly Dictionary<string, string> _words = [];

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

    public LanguageMessage GetLanguageMessage(EntityUid entity, string message, string? language)
    {
        if (language is null || TrySpeakTranslated(entity, message))
            return message;

        var words = GetWordRegex().Split(message);

        StringBuilder messageBuilder = new();

        StringBuilder wordBuilder = new();

        foreach (var match in GetWordRegex().Matches(message).Cast<Match>())
        {
            var word = match.Value;

            if (!GetLetterRegex().IsMatch([word[0]]))
            {
                messageBuilder.Append(word);
                continue;
            }

            var lowerWord = word.ToLower();

            if (!_words.TryGetValue(lowerWord, out var languageWord))
            {
                var syllablesCount = word.Count(letter => _vowels.Contains(char.ToLower(letter)));

                syllablesCount = syllablesCount switch
                {
                    1 => 2,
                    _ => _random.Next(3, 5)
                };

                var syllables = Syllables[language];

                for (var i = 0; i < syllablesCount; i++)
                    wordBuilder.Append(syllables[_random.Next(syllables.Length)]);

                languageWord = wordBuilder.ToString();

                wordBuilder.Clear();

                _words.Add(lowerWord, languageWord);
            }

            if (IsUpper(word))
                languageWord = languageWord.ToUpper();
            else if (char.IsUpper(word[0]))
                languageWord = char.ToUpper(languageWord[0]) + languageWord[1..];

            messageBuilder.Append(languageWord);
        }

        return new(message, language, messageBuilder.ToString());
    }

    private bool TrySpeakTranslated(EntityUid entity, string message)
    {
        return TryGetTranslator(_inventory.GetHandOrInventoryEntities(entity), out var translator) && _power.TryUseCharge(translator.Value, 0.2f * message.Length);
    }

    private bool TryGetTranslator(IEnumerable<EntityUid> entities, [NotNullWhen(true)] out EntityUid? translator)
    {
        foreach (var entity in entities)
        {
            if (EntityManager.HasComponent<LanguageTranslatorComponent>(entity))
            {
                translator = entity;
                return true;
            }

            if (EntityManager.TryGetComponent<StorageComponent>(entity, out var storage) && TryGetTranslator(storage.Container.ContainedEntities, out translator))
                return true;
        }

        translator = null;
        return false;
    }

    private static bool IsUpper(string str)
    {
        foreach (var c in str)
            if (char.IsLower(c))
                return false;

        return true;
    }
}

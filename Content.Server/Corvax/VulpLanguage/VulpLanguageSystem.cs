using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;
using Content.Server.PowerCell;
using Content.Server.VulpLangauge;
using Content.Shared.Inventory;
using Content.Shared.Storage;

namespace Content.Server.Corvax.VulpLanguage;

public sealed partial class VulpLanguageSystem : EntitySystem
{
    [GeneratedRegex(@"\b\w+\b|\W+")]
    private static partial Regex GetWordRegex();

    [GeneratedRegex(@"\w")]
    private static partial Regex GetLetterRegex();

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _power = default!;

    private readonly Dictionary<string, string> _words = [];

    private readonly string[] _syllables = ["рур", "йа", "цен", "раур", "бар", "кук", "тек", "кват", "ук", "ву", "вух", "тах", "тч", "счз", "ауч", "ист", "айн",
        "енщ", "звичз", "тут", "мир", "во", "бис", "ес", "вор", "ник", "гро", "ллл", "енем", "зандт", "тзч", "ноч", "хел", "ишт", "фар", "ва", "барам", "ийренг",
        "теч", "лач", "сам", "мак", "лич", "ген", "ор", "аг", "ецк", "гец", "стаг", "онн", "бин", "кет", "жарл", "вулф", "ейнеч", "црестхц", "азунайн", "гхзтх"];

    private readonly SearchValues<char> _vowels = SearchValues.Create("ауоиэыяюеё");

    private readonly Random _random = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageTransformEvent>(OnLanguageTransform);
        SubscribeLocalEvent<CheckLanguageUnderstandEvent>(OnCheckLanguageUnderstand);
    }

    private void OnLanguageTransform(LanguageTransformEvent e)
    {
        if (!EntityManager.HasComponent<VulpLanguageSpeakerComponent>(e.Sender))
            return;

        if (TryGetTranslator(e.Sender, out var translator) && _power.TryUseCharge(translator.Value, 0.2f * e.Message.Length))
            return;

        var words = GetWordRegex().Split(e.Message);

        StringBuilder messageBuilder = new();

        StringBuilder wordBuilder = new();

        foreach (var match in GetWordRegex().Matches(e.Message).Cast<Match>())
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

                for (var i = 0; i < syllablesCount; i++)
                    wordBuilder.Append(_syllables[_random.Next(_syllables.Length)]);

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

        e.Message = messageBuilder.ToString();
    }

    private static bool IsUpper(string str)
    {
        foreach (var c in str)
            if (char.IsLower(c))
                return false;

        return true;
    }

    private bool TryGetTranslator(EntityUid entity, [NotNullWhen(true)] out EntityUid? translator)
    {
        return TryGetTranslator(_inventory.GetHandOrInventoryEntities(entity), out translator);
    }

    private bool TryGetTranslator(IEnumerable<EntityUid> entities, [NotNullWhen(true)] out EntityUid? translator)
    {
        foreach (var entity in entities)
        {
            if (EntityManager.HasComponent<VulpTranslatorComponent>(entity))
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

    private void OnCheckLanguageUnderstand(CheckLanguageUnderstandEvent e)
    {
        if (!EntityManager.HasComponent<VulpLanguageSpeakerComponent>(e.Sender))
            return;

        if (EntityManager.HasComponent<VulpLanguageListenerComponent>(e.Listener))
            e.Understand = true;
    }
}

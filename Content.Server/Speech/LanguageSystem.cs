using System.Linq;
using System.Text;
using Content.Server.Chat.Systems;
using Content.Shared.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech;

public sealed class LanguageSystem : EntitySystem
{
    private static LanguagePrototype? _galacticCommon;
    private static LanguagePrototype? _universal;
    public static LanguagePrototype GalacticCommon { get => _galacticCommon!; }
    public static LanguagePrototype Universal { get => _universal!; }

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _prototype.TryIndex("GalacticCommon", out LanguagePrototype? gc);
        _prototype.TryIndex("Universal", out LanguagePrototype? universal);
        _galacticCommon = gc;
        _universal = universal;
        _sawmill = Logger.GetSawmill("language");

        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentInit>(OnInitLanguageSpeaker);
    }

    private void OnInitLanguageSpeaker(EntityUid uid, LanguageSpeakerComponent component, ComponentInit args)
    {
        if (component.SpokenLanguages.Count == 0)
        {
            throw new ArgumentException("Language speaker must speak at least one language.");
        }

        if (string.IsNullOrEmpty(component.CurrentLanguage))
        {
            component.CurrentLanguage = component.SpokenLanguages.First();
        };
    }

    /// <summary>
    ///     Obfuscate speech of the given entity, or using the given language.
    /// </summary>
    /// <param name="source">The speaker whose message needs to be obfuscated. Must not be null if "language" is not set.</param>
    /// <param name="language">The language for obfuscation. Must not be null if "source" is null.</param>
    public string ObfuscateSpeech(EntityUid? source, string message, LanguagePrototype? language = null)
    {
        if (language == null)
        {
            if (source == null)
            {
                throw new NullReferenceException("Either source or language must be set.");
            }
            language = GetLanguage(source.Value);
        }

        var builder = new StringBuilder();
        if (language.ObfuscateSyllables)
        {
            // Go through every syllable in every sentence and replace it with "replacement", preserving spaces
            // Effectively, the number of syllables in a word is equal to the number of vowels in it.
            for (var i = 0; i < message.Length; i++)
            {
                var ch = char.ToLower(message[i]);
                if (char.IsWhiteSpace(ch) || IsSentenceEnd(ch))
                {
                    builder.Append(ch); // This will preserve major punctuation and spaces
                }
                else if (IsVowel(ch))
                {
                    builder.Append(_random.Pick(language.Replacement));
                    _sawmill.Debug("Found a vowel: " + ch);
                }
            }
        }
        else
        {
            // Replace every sentence with "replacement"
            for (int i = 0; i < message.Length; i++)
            {
                var ch = char.ToLower(message[i]);
                if (IsSentenceEnd(ch))
                {
                    builder.Append(_random.Pick(language.Replacement));
                    builder.Append(' ');
                }

                // Skip any consequent sentence ends to account for !.., ?.., ?!, and similar
                while (i < message.Length - 1 && (IsSentenceEnd(message[i + 1]) || char.IsWhiteSpace(message[i + 1])))
                    i++;
            }

            // Finally, add one more string unless the last character is a sentence end
            if (IsSentenceEnd(message[^1]))
                builder.Append(_random.Pick(language.Replacement));
        }

        _sawmill.Info($"Got {message}, obfuscated to {builder}. Language: {language.ID}");

        return builder.ToString();
    }

    public bool CanUnderstand(EntityUid listener,
        LanguagePrototype language,
        LanguageSpeakerComponent? listenerLanguageComp = null)
    {
        if (language.ID == Universal.ID || HasComp<UniversalLanguageSpeakerComponent>(listener))
            return true;

        var listenerLanguages = GetLanguages(listener, listenerLanguageComp).UnderstoodLanguages;

        return listenerLanguages.Contains(language.ID, StringComparer.Ordinal);
    }

    public bool CanUnderstand(EntityUid speaker, EntityUid listener,
        LanguageSpeakerComponent? speakerComp = null,
        LanguageSpeakerComponent? listenerComp = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(listener) || HasComp<UniversalLanguageSpeakerComponent>(speaker))
            return true;

        var speakerLanguage = GetLanguages(speaker, speakerComp).CurrentLanguage;
        if (speakerLanguage == Universal.ID)
            return true;
        var listenerLanguages = GetLanguages(listener, listenerComp).UnderstoodLanguages;

        return listenerLanguages.Contains(speakerLanguage, StringComparer.Ordinal);
    }

    // <summary>
    //     Returns the current language of the given entity. Assumes Universal if not specified.
    // </summary>
    public LanguagePrototype GetLanguage(EntityUid speaker, LanguageSpeakerComponent? languageComp = null)
    {
        var id = GetLanguages(speaker, languageComp).CurrentLanguage;
        _prototype.TryIndex(id, out LanguagePrototype? proto);

        return proto ?? Universal;
    }

    private bool IsVowel(char ch)
    {
        // This is not a language-agnostic approach and will totally break with non-latin languages.
        return ch is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
    }

    private bool IsSentenceEnd(char ch)
    {
        return ch is '.' or '!' or '?';
    }

    // This event is reused because re-allocating it each time is way too costly.
    private DetermineEntityLanguagesEvent _determineLanguagesEvent = new("", new(), new());

    /// <summary>
    ///   Dynamically resolves the current language of the entity and the list of all languages it speaks.
    ///   The returned event is reused and thus must not be held as a reference anywhere but inside the caller function.
    /// </summary>
    private DetermineEntityLanguagesEvent GetLanguages(EntityUid speaker, LanguageSpeakerComponent? comp = null)
    {
        var ev = _determineLanguagesEvent;
        ev.CurrentLanguage = Universal.ID;
        ev.SpokenLanguages.Clear();
        ev.UnderstoodLanguages.Clear();

        if (comp != null || TryComp(speaker, out comp))
        {
            ev.CurrentLanguage = comp.CurrentLanguage;
            ev.SpokenLanguages.AddRange(comp.SpokenLanguages);
            ev.UnderstoodLanguages.AddRange(comp.UnderstoodLanguages);
        }

        RaiseLocalEvent(speaker, ev);

        return ev;
    }

    /// <summary>
    ///   Raised in order to determine the language an entity speaks at the current moment,
    ///   as well as the list of all languages the entity may speak and understand.
    /// </summary>
    public sealed class DetermineEntityLanguagesEvent : EntityEventArgs
    {
        public string CurrentLanguage;
        /// <summary>
        ///   The list of all languages the entity may speak. Must NOT be held as a reference!
        /// </summary>
        public List<string> SpokenLanguages;
        /// <summary>
        ///   The list of all languages the entity may understand. Must NOT be held as a reference!
        /// </summary>
        public List<string> UnderstoodLanguages;

        public DetermineEntityLanguagesEvent(string currentLanguage, List<string> spokenLanguages, List<string> understoodLanguages)
        {
            CurrentLanguage = currentLanguage;
            SpokenLanguages = spokenLanguages;
            UnderstoodLanguages = understoodLanguages;
        }
    }
}

using System.Linq;
using System.Text;
using Content.Server.Chat.Systems;
using Content.Shared.Language;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Language;

public sealed class LanguageSystem : SharedLanguageSystem
{

    public override void Initialize()
    {
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
        }
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

        // TODO: REWORK THIS!
        // Should instead use random number of syllables/phrases per word/sentence depending on its length
        // Also preferably should use a simple hash code of the word as the random seed to make obfuscation stable
        // This will also allow people to learn certain phrases, e.g. how "yes" is spelled in canilunzt.
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

        var listenerLanguages = GetLanguages(listener, listenerLanguageComp)?.UnderstoodLanguages;

        return listenerLanguages?.Contains(language.ID, StringComparer.Ordinal) ?? false;
    }

    public bool CanSpeak(EntityUid speaker, string language, LanguageSpeakerComponent? speakerComp = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(speaker))
            return true;

        var langs = GetLanguages(speaker, speakerComp)?.UnderstoodLanguages;
        return langs?.Contains(language, StringComparer.Ordinal) ?? false;
    }

    // <summary>
    //     Returns the current language of the given entity. Assumes Universal if not specified.
    // </summary>
    public LanguagePrototype GetLanguage(EntityUid speaker, LanguageSpeakerComponent? languageComp = null)
    {
        var id = GetLanguages(speaker, languageComp)?.CurrentLanguage;
        if (id == null)
            return Universal; // Fallback

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
    private DetermineEntityLanguagesEvent _determineLanguagesEvent = new(string.Empty, new(), new());

    /// <summary>
    ///   Returns a pair of (spoken, understood) languages of the given entity.
    /// </summary>
    public (List<string>, List<string>) GetAllLanguages(EntityUid speaker)
    {
        var languages = GetLanguages(speaker);
        if (languages == null)
            return (new(), new());

        // The lists need to be copied because the internal ones are re-used for performance reasons.
        return (new List<string>(languages.SpokenLanguages), new List<string>(languages.UnderstoodLanguages));
    }

    /// <summary>
    ///   Dynamically resolves the current language of the entity and the list of all languages it speaks.
    ///   The returned event is reused and thus must not be held as a reference anywhere but inside the caller function.
    /// </summary>
    private DetermineEntityLanguagesEvent? GetLanguages(EntityUid speaker, LanguageSpeakerComponent? comp = null)
    {
        if (comp == null && !TryComp(speaker, out comp))
            return null;

        var ev = _determineLanguagesEvent;
        ev.SpokenLanguages.Clear();
        ev.UnderstoodLanguages.Clear();

        ev.CurrentLanguage = comp.CurrentLanguage;
        ev.SpokenLanguages.AddRange(comp.SpokenLanguages);
        ev.UnderstoodLanguages.AddRange(comp.UnderstoodLanguages);

        RaiseLocalEvent(speaker, ev, true);

        if (ev.CurrentLanguage.Length == 0)
            ev.CurrentLanguage = comp?.CurrentLanguage ?? Universal.ID; // Fall back to account for admemes like admins possessing a bread
        return ev;
    }

    /// <summary>
    ///   Raised in order to determine the language an entity speaks at the current moment,
    ///   as well as the list of all languages the entity may speak and understand.
    /// </summary>
    public sealed class DetermineEntityLanguagesEvent : EntityEventArgs
    {
        /// <summary>
        ///   The default language of this entity. If empty, remain unchanged.
        ///   This field has no effect if the entity decides to speak in a concrete language.
        /// </summary>
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

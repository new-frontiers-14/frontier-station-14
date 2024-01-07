using System.Linq;
using System.Text;
using Content.Shared.GameTicking;
using Content.Shared.Language;
using Content.Shared.Language.Systems;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using UniversalLanguageSpeakerComponent = Content.Shared.Language.Components.UniversalLanguageSpeakerComponent;

namespace Content.Server.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    /// <summary>
    ///   A random number added to each pseudo-random number's seed. Changes every round.
    /// </summary>
    public int RandomRoundSeed { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentInit>(OnInitLanguageSpeaker);
        SubscribeAllEvent<RoundStartedEvent>(it => RandomRoundSeed = _random.Next());

        InitializeWindows();
    }

    private void OnInitLanguageSpeaker(EntityUid uid, LanguageSpeakerComponent component, ComponentInit args)
    {
        if (string.IsNullOrEmpty(component.CurrentLanguage))
        {
            component.CurrentLanguage = component.SpokenLanguages.FirstOrDefault(UniversalPrototype);
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
            if (source is not { Valid: true })
            {
                throw new NullReferenceException("Either source or language must be set.");
            }
            language = GetLanguage(source.Value);
        }

        var builder = new StringBuilder();
        if (language.ObfuscateSyllables)
        {
            ObfuscateSyllables(builder, message, language);
        }
        else
        {
            ObfuscatePhrases(builder, message, language);
        }

        //_sawmill.Info($"Got {message}, obfuscated to {builder}. Language: {language.ID}");

        return builder.ToString();
    }

    private void ObfuscateSyllables(StringBuilder builder, string message, LanguagePrototype language)
    {
        // Go through each word. Calculate its hash sum and count the number of letters.
        // Replicate it with pseudo-random syllables of pseudo-random (but similar) length. Use the hash code as the seed.
        // This means that identical words will be obfuscated identically. Simple words like "hello" or "yes" in different langs can be memorized.
        var wordBeginIndex = 0;
        var hashCode = 0;
        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            // A word ends when one of the following is found: a space, a sentence end, or EOM
            if (char.IsWhiteSpace(ch) || IsSentenceEnd(ch) || i == message.Length - 1)
            {
                var wordLength = i - wordBeginIndex;
                if (wordLength > 0)
                {
                    var newWordLength = PseudoRandomNumber(hashCode, 1, 4);

                    for (var j = 0; j < newWordLength; j++)
                    {
                        var index = PseudoRandomNumber(hashCode + j, 0, language.Replacement.Count);
                        builder.Append(language.Replacement[index]);
                    }
                }

                builder.Append(ch);
                hashCode = 0;
                wordBeginIndex = i + 1;
            }
            else
            {
                hashCode = hashCode * 31 + ch;
            }
        }
    }

    private void ObfuscatePhrases(StringBuilder builder, string message, LanguagePrototype language)
    {
        // In a similar manner, each phrase is obfuscated with a random number of conjoined obfuscation phrases.
        // However, the number of phrases depends on the number of characters in the original phrase.
        var sentenceBeginIndex = 0;
        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            if (IsSentenceEnd(ch) || i == message.Length - 1)
            {
                var length = i - sentenceBeginIndex;
                if (length > 0)
                {
                    var newLength = (int) Math.Clamp(Math.Cbrt(length) - 1, 1, 4); // 27+ chars for 2 phrases, 64+ for 3, 125+ for 4.

                    for (var j = 0; j < newLength; j++)
                    {
                        var phrase = _random.Pick(language.Replacement);
                        builder.Append(phrase);
                    }
                }
                sentenceBeginIndex = i + 1;

                if (IsSentenceEnd(ch))
                    builder.Append(ch).Append(" ");
            }
        }
    }

    public bool CanUnderstand(EntityUid listener,
        LanguagePrototype language,
        LanguageSpeakerComponent? listenerLanguageComp = null)
    {
        if (language.ID == UniversalPrototype || HasComp<UniversalLanguageSpeakerComponent>(listener))
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

    // <summary>
    //     Set the CurrentLanguage of the given entity.
    // </summary>
    public void SetLanguage(EntityUid speaker, string language, LanguageSpeakerComponent? languageComp = null)
    {
        if (!CanSpeak(speaker, language))
            return;

        if (languageComp == null && !TryComp(speaker, out languageComp))
            return;

        if (languageComp.CurrentLanguage == language)
            return;

        languageComp.CurrentLanguage = language;

        RaiseLocalEvent(speaker, new LanguagesUpdateEvent(), true);
    }

    /// <summary>
    ///   Adds a new language to the lists of understood and/or spoken languages of the given component.
    /// </summary>
    public void AddLanguage(LanguageSpeakerComponent comp, string language, bool addSpoken = true, bool addUnderstood = true)
    {
        if (addSpoken && !comp.SpokenLanguages.Contains(language, StringComparer.Ordinal))
            comp.SpokenLanguages.Add(language);

        if (addUnderstood && !comp.UnderstoodLanguages.Contains(language, StringComparer.Ordinal))
            comp.UnderstoodLanguages.Add(language);

        RaiseLocalEvent(comp.Owner, new LanguagesUpdateEvent(), true);
    }

    private static bool IsSentenceEnd(char ch)
    {
        return ch is '.' or '!' or '?';
    }

    // This event is reused because re-allocating it each time is way too costly.
    private readonly DetermineEntityLanguagesEvent _determineLanguagesEvent = new(string.Empty, new(), new());

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
            ev.CurrentLanguage = !string.IsNullOrEmpty(comp.CurrentLanguage) ? comp.CurrentLanguage : UniversalPrototype; // Fall back to account for admemes like admins possessing a bread
        return ev;
    }

    /// <summary>
    ///   Generates a stable pseudo-random number in the range [min, max) for the given seed. Each input seed corresponds to exactly one random number.
    /// </summary>
    private int PseudoRandomNumber(int seed, int min, int max)
    {
        // This is not a uniform distribution, but it shouldn't matter: given there's 2^31 possible random numbers,
        // The bias of this function should be so tiny it will never be noticed.
        seed += RandomRoundSeed;
        var random = ((seed * 1103515245) + 12345) & 0x7fffffff; // Source: http://cs.uccs.edu/~cs591/bufferOverflow/glibc-2.2.4/stdlib/random_r.c
        return random % (max - min) + min;
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    public void EnsureValidLanguage(EntityUid entity, LanguageSpeakerComponent? comp = null)
    {
        if (comp == null && !TryComp(entity, out comp))
            return;

        var langs = GetLanguages(entity, comp);

        if (langs != null && !langs.SpokenLanguages.Contains(comp!.CurrentLanguage, StringComparer.Ordinal))
        {
            comp.CurrentLanguage = langs.SpokenLanguages.FirstOrDefault(UniversalPrototype);
        }
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

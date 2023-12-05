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
        if (component.Languages.Count == 0)
        {
            throw new ArgumentException("Language speaker must know at least one language.");
        }

        if (string.IsNullOrEmpty(component.CurrentLanguage))
        {
            component.CurrentLanguage = component.Languages.First();
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
                var ch = message[i];
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
                var ch = message[i];
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

        _sawmill.Info($"Got {message}, obfuscated to {builder.ToString()}. Language: {language.ID}, replacements: {string.Join(", ", language.Replacement)}");

        return builder.ToString();
    }

    public bool CanUnderstand(EntityUid listener,
        LanguagePrototype language,
        LanguageSpeakerComponent? listenerLanguageComp = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(listener))
        {
            return true;
        }

        if (listenerLanguageComp == null && !TryComp(listener, out listenerLanguageComp))
        {
            return false;
        }

        return language.ID == listenerLanguageComp.CurrentLanguage ||
               listenerLanguageComp.Languages.Contains(language.ID, StringComparer.Ordinal);
    }

    public bool CanUnderstand(EntityUid speaker, EntityUid listener,
        LanguageSpeakerComponent? speakerLanguage = null,
        LanguageSpeakerComponent? listenerLanguage = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(listener) || HasComp<UniversalLanguageSpeakerComponent>(speaker))
        {
            return true;
        }

        if (speakerLanguage == null && !TryComp(speaker, out speakerLanguage))
        {
            return false;
        }

        if (listenerLanguage == null && !TryComp(listener, out listenerLanguage))
        {
            return false;
        }

        return listenerLanguage.Languages.Contains(speakerLanguage.CurrentLanguage, StringComparer.Ordinal);
    }

    // <summary>
    //     Returns the current language of the given entity. Assumes GalacticCommon if not specified.
    // </summary>
    public LanguagePrototype GetLanguage(EntityUid speaker, LanguageSpeakerComponent? languageComp = null)
    {
        if (languageComp != null || TryComp(speaker, out languageComp))
        {
            return _prototype.Index<LanguagePrototype>(languageComp.CurrentLanguage);
        }
        // Fall back if the entity is not a language speaker.
        // This may include breads, doors, walls - anything an admin can decide to possess
        // TODO: may want to use Universal instead!
        return GalacticCommon;
    }

    private bool HasIntersectingLanguages(LanguageSpeakerComponent speaker, LanguageSpeakerComponent listener)
    {
        return listener != null && listener.Languages.Contains(speaker.CurrentLanguage, StringComparer.Ordinal);
    }

    private bool IsVowel(char ch)
    {
        // This is not a language-agnostic approach and will totally break with non-latin languages.
        return ch == 'a' || ch == 'e' || ch == 'i' || ch == 'o' || ch == 'u' || ch == 'y';
    }

    private bool IsSentenceEnd(char ch)
    {
        return ch == '.' || ch == '!' || ch == '?';
    }
}

using System.Runtime.CompilerServices;
using Robust.Shared.Prototypes;

namespace Content.Shared.Language;

[Prototype("language")]
public sealed class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set;  } = default!;

    // <summary>
    // If true, obfuscated phrases of creatures speaking this language will have their syllables replaced with "replacement" syllables.
    // Otherwise entire sentences will be replaced.
    // </summary>
    [DataField("obfuscateSyllables", required: true)]
    public bool ObfuscateSyllables { get; private set; } = false;

    // <summary>
    // Lists all syllables that are used to obfuscate a message a listener cannot understand if obfuscateSyllables is true,
    // Otherwise uses all possible phrases the creature can make when trying to say anything.
    // </summary>
    [DataField("replacement", required: true)]
    public List<string> Replacement = new();

    #region utility

    public string LocalizedName => GetLocalizedName(ID);

    public string LocalizedDescription => GetLocalizedDescription(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLocalizedName(string languageId) =>
        Loc.GetString("language-" + languageId + "-name");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLocalizedDescription(string languageId) =>
        Loc.GetString("language-" + languageId + "-description");

    #endregion utility
}

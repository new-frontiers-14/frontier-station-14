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

    public string LocalizedName => Loc.GetString("language-" + ID + "-name");

    public string LocalizedDescription => Loc.GetString("language-" + ID + "-description");
}

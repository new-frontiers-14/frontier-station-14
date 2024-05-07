namespace Content.Server.Corvax.Language;

public readonly record struct LanguageMessage(string OriginalMessage, string? Language, string Message)
{
    public static implicit operator LanguageMessage(string str)
    {
        return new(str, null, str);
    }
}

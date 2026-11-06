using System.Linq;
using System.Text;
using Content.Shared.Guidebook;

namespace Content.Client._NF.Guidebook;

/// <summary>
/// Cached, localized page titles and aliases. Adapted from Starlight #6822.
/// </summary>
internal sealed class GuidebookSearchEntry
{
    /// <summary>
    /// Normalized title and keyword phrases cached for matching.
    /// </summary>
    private readonly string[] _terms;

    /// <summary>
    /// Caches the localized title and alternative search phrases without case or separators.
    /// </summary>
    public GuidebookSearchEntry(string name, IEnumerable<string> keywords)
    {
        _terms = keywords.Prepend(name).Select(Normalize).Where(term => term.Length > 0).ToArray();
    }

    /// <summary>
    /// Combines the localized title, shared keyword catalog, and additional prototype keywords.
    /// </summary>
    public static GuidebookSearchEntry FromEntry(GuideEntry entry, ILocalizationManager localization)
    {
        var keywords = entry.SearchKeywords.Select(keyword => localization.GetString(keyword));
        if (localization.TryGetString($"nf-guidebook-search-keywords-{entry.Id.ToLowerInvariant()}", out var aliases))
        {
            keywords = keywords.Concat(aliases.Split(';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return new GuidebookSearchEntry(localization.GetString(entry.Name), keywords);
    }

    /// <summary>
    /// Matches phrases or all query words, ignoring case, punctuation, and word order.
    /// </summary>
    public bool Matches(string query)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0 || _terms.Any(term => term.Contains(normalized, StringComparison.Ordinal)))
            return true;

        // Query words can match different terms, including both the title and its aliases.
        var separated = new string(query
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ').ToArray());
        return separated.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(word => _terms.Any(term => term.Contains(word, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Keeps only lowercase letters and digits so joined words and spaced phrases can match.
    /// </summary>
    private static string Normalize(string text)
    {
        var normalized = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
                normalized.Append(char.ToLowerInvariant(c));
        }

        return normalized.ToString();
    }
}

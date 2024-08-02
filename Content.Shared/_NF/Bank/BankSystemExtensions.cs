using System.Globalization;

namespace Content.Shared._NF.Bank;

public static class BankSystemExtensions
{
    /// <summary>
    /// Formats a integer to the current CultureInfo's number formatting for currency.
    /// </summary>
    /// <param name="amount">The amount to format</param>
    /// <param name="culture">The optional culture to use for formatting</param>
    /// <param name="symbolOverride">Optionally override the symbol</param>
    /// <param name="separatorOverride">Optionally override the separator</param>
    /// <returns></returns>
    public static string ToCurrencyString(int amount, CultureInfo? culture = null, string? symbolOverride = null, string? separatorOverride = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();

        if (symbolOverride != null)
        {
            numberFormat.CurrencySymbol = symbolOverride;
        }
        if (separatorOverride != null)
        {
            numberFormat.CurrencyGroupSeparator = separatorOverride;
        }


        return string.Format(numberFormat, "{0:C0}", amount);
    }
}


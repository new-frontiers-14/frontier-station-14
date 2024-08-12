using System.Globalization;

namespace Content.Shared._NF.Bank;

public static class BankSystemExtensions
{

    public enum CurrencySymbolLocation
    {
        Default, // Dependent on local CultureInfo
        Prefix, // Currency symbol goes before the number
        Suffix // Currency symbols goes after the number
    }

    const int PrefixCurrencyPositivePattern = 0; //$N
    const int PrefixCurrencyNegativePattern = 1; //-$N
    const int SuffixCurrencyPositivePattern = 3; //N $
    const int SuffixCurrencyNegativePattern = 8; //-N $

    /// <summary>
    /// Formats a integer to the current CultureInfo's number formatting for currency.
    /// </summary>
    /// <param name="amount">The amount to format</param>
    /// <param name="culture">The optional culture to use for formatting</param>
    /// <param name="symbolOverride">Optionally override the symbol</param>
    /// <param name="separatorOverride">Optionally override the separator</param>
    /// <returns></returns>
    public static string ToCurrencyString(int amount, CultureInfo? culture = null, string? symbolOverride = null, string? separatorOverride = null, CurrencySymbolLocation symbolLocation = CurrencySymbolLocation.Default)
    {
        culture ??= CultureInfo.CurrentCulture;
        var numberFormat = (NumberFormatInfo) culture.NumberFormat.Clone();

        if (symbolOverride != null)
        {
            numberFormat.CurrencySymbol = symbolOverride;
        }
        if (separatorOverride != null)
        {
            numberFormat.CurrencyGroupSeparator = separatorOverride;
        }
        switch (symbolLocation)
        {
            case CurrencySymbolLocation.Default:
                break; // Do nothing
            case CurrencySymbolLocation.Prefix:
                numberFormat.CurrencyPositivePattern = PrefixCurrencyPositivePattern;
                numberFormat.CurrencyNegativePattern = PrefixCurrencyNegativePattern;
                break;
            case CurrencySymbolLocation.Suffix:
                numberFormat.CurrencyPositivePattern = SuffixCurrencyPositivePattern;
                numberFormat.CurrencyNegativePattern = SuffixCurrencyNegativePattern;
                break;
        }


        return string.Format(numberFormat, "{0:C0}", amount);
    }

    // Convenience methods for specific currencies.
    public static string ToIndependentString(int amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "", symbolLocation: CurrencySymbolLocation.Prefix); //Prefix results in no space, prefer that.
    }

    public static string ToSpesoString(int amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "$", symbolLocation: CurrencySymbolLocation.Prefix);
    }

    public static string ToDoubloonString(int amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "DB", symbolLocation: CurrencySymbolLocation.Suffix);
    }

    public static string ToFUCString(int amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "FUC", symbolLocation: CurrencySymbolLocation.Suffix);
    }
}


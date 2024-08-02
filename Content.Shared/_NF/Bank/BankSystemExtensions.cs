namespace Content.Shared._NF.Bank;

public static class BankSystemExtensions
{
    public static string ToCurrencyString(int amount)
    {
        return $"${amount:N0}".Replace(",", ".");
    }
}


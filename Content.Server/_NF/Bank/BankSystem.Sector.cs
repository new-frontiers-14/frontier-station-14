using System.Runtime.InteropServices;
using Content.Server._NF.SectorServices;
using Content.Shared.Bank;
using Content.Shared.Bank.Components;
using JetBrains.Annotations;

namespace Content.Server.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;

    // The interval between sector account increases, in seconds.
    private const float AccountIncreaseInterval = 10.0f;

    /// <summary>
    /// Attempts to remove money from a character's bank account.
    /// This should always be used instead of attempting to modify the BankAccountComponent directly.
    /// When successful, the entity's BankAccountComponent will be updated with their current balance.
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is attached to, typically the player controlled mob</param>
    /// <param name="amount">The integer amount of which to decrease the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
    [PublicAPI]
    public bool TryBankWithdraw(SectorBankAccount account, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"TryBankWithdraw: {amount} is invalid");
            return false;
        }

        // Lookup sector banks
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? bank))
        {
            _log.Info($"TryBankWithdraw: no bank component");
            return false;
        }

        if (!bank.Accounts.ContainsKey(account))
        {
            _log.Info($"TryBankWithdraw: invalid account");
            return false;
        }

        var bankAccount = CollectionsMarshal.GetValueRefOrNullRef(bank.Accounts, account);
        if (bankAccount.Balance < amount)
        {
            _log.Info($"TryBankWithdraw: account has less money {bankAccount.Balance} than requested {amount}");
            return false;
        }

        bankAccount.Balance -= amount;
        return true;
    }

    /// <summary>
    /// Attempts to add money to a character's bank account. This should always be used instead of attempting to modify the bankaccountcomponent directly
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="amount">The amount of spesos to remove from the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
    [PublicAPI]
    public bool TryBankDeposit(SectorBankAccount account, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"TryBankDeposit: {amount} is invalid");
            return false;
        }

        // Lookup sector banks
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? bank))
        {
            _log.Info($"TryBankDeposit: no bank component");
            return false;
        }

        if (!bank.Accounts.ContainsKey(account))
        {
            _log.Info($"TryBankDeposit: invalid account");
            return false;
        }

        var bankAccount = CollectionsMarshal.GetValueRefOrNullRef(bank.Accounts, account);
        bankAccount.Balance += amount;
        return true;
    }

    /// <summary>
    /// Retrieves a character's balance via its in-game entity, if it has one.
    /// </summary>
    /// <param name="ent">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="balance">When successful, contains the account balance in spesos. Otherwise, set to 0.</param>
    /// <returns>true if the account was successfully queried.</returns>
    [PublicAPI]
    public bool TryGetBalance(SectorBankAccount account, out int balance)
    {
        // Lookup sector banks
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? bank))
        {
            _log.Info($"TryGetBalance: no bank component");
            balance = 0;
            return false;
        }

        if (!bank.Accounts.ContainsKey(account))
        {
            _log.Info($"TryGetBalance: invalid account");
            balance = 0;
            return false;
        }

        balance = bank.Accounts[account].Balance;
        return true;
    }


    private void UpdateSectorBanks(float frameTime)
    {
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? bank))
            return;

        bank.SecondsSinceLastIncrease += frameTime;

        float secondsToCredit = 0;
        while (bank.SecondsSinceLastIncrease > AccountIncreaseInterval)
        {
            bank.SecondsSinceLastIncrease -= AccountIncreaseInterval;
            secondsToCredit++;
        }

        int seconds = (int)secondsToCredit;
        if (seconds <= 0)
            return;

        foreach (var accountId in bank.Accounts.Keys)
        {
            var accountRef = CollectionsMarshal.GetValueRefOrNullRef(bank.Accounts, accountId);
            accountRef.Balance += seconds * accountRef.IncreasePerSecond;
        }
    }
}

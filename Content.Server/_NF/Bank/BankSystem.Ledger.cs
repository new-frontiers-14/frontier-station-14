using System.Text;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Components;

namespace Content.Server._NF.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    public void CleanupLedger()
    {
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return;
        ledger.AccountLedgerEntries.Clear();
    }

    // Adds an entry to the ledger.
    // Only positive amounts are added.
    public void AddLedgerEntry(SectorBankAccount account, LedgerEntryType entryType, int amount)
    {
        if (amount <= 0)
            return;
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return;

        var tuple = (account, entryType);
        if (ledger.AccountLedgerEntries.ContainsKey(tuple))
            ledger.AccountLedgerEntries[tuple] += amount;
        else
            ledger.AccountLedgerEntries[tuple] = amount;
        RaiseLocalEvent(new SectorLedgerUpdatedEvent());
    }

    sealed class AccountInfo
    {
        public int TotalIncome;
        public int TotalExpenses;
        public List<(LedgerEntryType Type, int Value)> Income = new();
        public List<(LedgerEntryType Type, int Value)> Expenses = new();
    }

    public string GetLedgerPrintout()
    {
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return string.Empty;

        StringBuilder builder = new();

        // Group ledger entries by account
        Dictionary<SectorBankAccount, AccountInfo> accountDict = new();
        foreach (var value in Enum.GetValues<SectorBankAccount>())
        {
            if (value == SectorBankAccount.Invalid)
                continue;
            accountDict[value] = new AccountInfo();
        }
        foreach (var (ledgerEntry, value) in ledger.AccountLedgerEntries)
        {
            if (!accountDict.ContainsKey(ledgerEntry.Account))
                continue;
            if (ledgerEntry.Type >= LedgerEntryType.FirstExpense)
            {
                accountDict[ledgerEntry.Account].Expenses.Add((ledgerEntry.Type, value));
                accountDict[ledgerEntry.Account].TotalExpenses += value;
            }
            else
            {
                accountDict[ledgerEntry.Account].Income.Add((ledgerEntry.Type, value));
                accountDict[ledgerEntry.Account].TotalIncome += value;
            }
        }

        // Build our printouts
        foreach (var (account, accountInfo) in accountDict)
        {
            builder.AppendLine(Loc.GetString("ledger-printout-account", ("account", Loc.GetString($"ledger-tab-{account}"))));
            builder.AppendLine(Loc.GetString("ledger-printout-income-header"));
            foreach (var income in accountInfo.Income)
            {
                builder.AppendLine(
                    Loc.GetString("ledger-printout-line-item",
                        ("entryType", Loc.GetString($"ledger-entry-type-{income.Type}")),
                        ("amount", BankSystemExtensions.ToSpesoString(income.Value))
                    ));
            }
            builder.AppendLine(
                Loc.GetString("ledger-printout-total-income",
                    ("amount", BankSystemExtensions.ToSpesoString(accountInfo.TotalIncome))
                ));
            builder.AppendLine();
            builder.AppendLine(Loc.GetString("ledger-printout-expense-header"));
            foreach (var expense in accountInfo.Expenses)
            {
                builder.AppendLine(
                    Loc.GetString("ledger-printout-line-item",
                        ("entryType", Loc.GetString($"ledger-entry-type-{expense.Type}")),
                        ("amount", BankSystemExtensions.ToSpesoString(expense.Value))
                    ));
            }
            builder.AppendLine(
                Loc.GetString("ledger-printout-total-expenses",
                    ("amount", BankSystemExtensions.ToSpesoString(accountInfo.TotalExpenses))
                ));
            builder.AppendLine(
                Loc.GetString("ledger-printout-balance",
                    ("amount", BankSystemExtensions.ToSpesoString(accountInfo.TotalIncome - accountInfo.TotalExpenses))
                ));
            builder.AppendLine();
        }
        return builder.ToString();
    }
}

public sealed class SectorLedgerUpdatedEvent : EntityEventArgs;

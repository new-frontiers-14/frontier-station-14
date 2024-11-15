using System.Threading;
using Content.Server.Preferences.Managers;
using Content.Server.GameTicking;
using Content.Shared.Bank;
using Content.Shared.Bank.Components;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._NF.Bank.Events;

namespace Content.Server.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("bank");
        InitializeATM();
        InitializeStationATM();

        SubscribeLocalEvent<BankAccountComponent, PreferencesLoadedEvent>(OnPreferencesLoaded); // For late-add bank accounts
        SubscribeLocalEvent<BankAccountComponent, ComponentInit>(OnInit); // For late-add bank accounts
        SubscribeLocalEvent<BankAccountComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BankAccountComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerLobbyJoin);
    }

    /// <summary>
    /// Attempts to remove money from a character's bank account.
    /// This should always be used instead of attempting to modify the BankAccountComponent directly.
    /// When successful, the entity's BankAccountComponent will be updated with their current balance.
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is attached to, typically the player controlled mob</param>
    /// <param name="amount">The integer amount of which to decrease the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
    public bool TryBankWithdraw(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"TryBankWithdraw: {amount} is invalid");
            return false;
        }

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"TryBankWithdraw: {mobUid} has no bank account");
            return false;
        }

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session))
        {
            _log.Info($"TryBankWithdraw: {mobUid} has no attached session");
            return false;
        }

        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"TryBankWithdraw: {mobUid} has no cached prefs");
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"TryBankWithdraw: {mobUid} has the wrong prefs type");
            return false;
        }

        if (TryBankWithdraw(session, prefs, profile, amount, out var newBalance))
        {
            bank.Balance = newBalance.Value;
            Dirty(mobUid, bank);
            _log.Info($"{mobUid} withdrew {amount}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to add money to a character's bank account. This should always be used instead of attempting to modify the bankaccountcomponent directly
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="amount">The amount of spesos to remove from the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
    public bool TryBankDeposit(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"TryBankDeposit: {amount} is invalid");
            return false;
        }

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"TryBankDeposit: {mobUid} has no bank account");
            return false;
        }

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session))
        {
            _log.Info($"TryBankDeposit: {mobUid} has no attached session");
            return false;
        }

        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"TryBankDeposit: {mobUid} has no cached prefs");
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"TryBankDeposit: {mobUid} has the wrong prefs type");
            return false;
        }

        if (TryBankDeposit(session, prefs, profile, amount, out var newBalance))
        {
            bank.Balance = newBalance.Value;
            Dirty(mobUid, bank);
            _log.Info($"{mobUid} deposited {amount}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove money from a character's bank account without a backing entity.
    /// This should only be used in cases where a character doesn't have a backing entity.
    /// </summary>
    /// <param name="session">The session of the player making the withdrawal.</param>
    /// <param name="prefs">The preferences storing the character whose bank will be changed.</param>
    /// <param name="profile">The profile of the character whose account is being withdrawn.</param>
    /// <param name="amount">The number of spesos to be withdrawn.</param>
    /// <param name="newBalance">The new value of the bank account.</param>
    /// <returns>true if the transaction was successful, false if it was not.  When successful, newBalance contains the character's new balance.</returns>
    public bool TryBankWithdraw(ICommonSession session, PlayerPreferences prefs, HumanoidCharacterProfile profile, int amount, [NotNullWhen(true)] out int? newBalance)
    {
        newBalance = null; // Default return
        if (amount <= 0)
        {
            _log.Info($"TryBankWithdraw: {amount} is invalid");
            return false;
        }

        int balance = profile.BankBalance;

        if (balance < amount)
        {
            _log.Info($"TryBankWithdraw: {session.UserId} tried to withdraw {amount}, but has insufficient funds ({balance})");
            return false;
        }

        balance -= amount;

        var newProfile = profile.WithBankBalance(balance);
        var index = prefs.IndexOfCharacter(profile);
        if (index == -1)
        {
            _log.Info($"TryBankWithdraw: {session.UserId} tried to adjust the balance of {profile.Name}, but they were not in the user's character set.");
            return false;
        }
        _prefsManager.SetProfile(session.UserId, index, newProfile);
        newBalance = balance;
        // Update any active admin UI with new balance
        RaiseLocalEvent(new BalanceChangedEvent(session, newBalance.Value));
        return true;
    }

    /// <summary>
    /// Attempts to add money to a character's bank account.
    /// This should only be used in cases where a character doesn't have a backing entity.
    /// </summary>
    /// <param name="session">The session of the player making the deposit.</param>
    /// <param name="prefs">The preferences storing the character whose bank will be changed.</param>
    /// <param name="profile">The profile of the character whose account is being withdrawn.</param>
    /// <param name="amount">The number of spesos to be deposited.</param>
    /// <param name="newBalance">The new value of the bank account.</param>
    /// <returns>true if the transaction was successful, false if it was not.  When successful, newBalance contains the character's new balance.</returns>
    public bool TryBankDeposit(ICommonSession session, PlayerPreferences prefs, HumanoidCharacterProfile profile, int amount, [NotNullWhen(true)] out int? newBalance)
    {
        newBalance = null; // Default return
        if (amount <= 0)
        {
            _log.Info($"TryBankDeposit: {amount} is invalid");
            return false;
        }

        newBalance = profile.BankBalance + amount;

        var newProfile = profile.WithBankBalance(newBalance.Value);
        var index = prefs.IndexOfCharacter(profile);
        if (index == -1)
        {
            _log.Info($"{session.UserId} tried to adjust the balance of {profile.Name}, but they were not in the user's character set.");
            return false;
        }
        _prefsManager.SetProfile(session.UserId, index, newProfile);
        // Update any active admin UI with new balance
        RaiseLocalEvent(new BalanceChangedEvent(session, newBalance.Value));
        return true;
    }

    /// <summary>
    /// Retrieves a character's balance via its in-game entity, if it has one.
    /// </summary>
    /// <param name="ent">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="balance">When successful, contains the account balance in spesos. Otherwise, set to 0.</param>
    /// <returns>true if the account was successfully queried.</returns>
    public bool TryGetBalance(EntityUid ent, out int balance)
    {
        if (!_playerManager.TryGetSessionByEntity(ent, out var session) ||
            !_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"{ent} has no cached prefs");
            balance = 0;
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"{ent} has the wrong prefs type");
            balance = 0;
            return false;
        }

        balance = profile.BankBalance;
        return true;
    }

    /// <summary>
    /// Retrieves a character's balance via a player's session.
    /// </summary>
    /// <param name="session">The session of the player character to query.</param>
    /// <param name="balance">When successful, contains the account balance in spesos. Otherwise, set to 0.</param>
    /// <returns>true if the account was successfully queried.</returns>
    public bool TryGetBalance(ICommonSession session, out int balance)
    {
        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"{session.UserId} has no cached prefs");
            balance = 0;
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"{session.UserId} has the wrong prefs type");
            balance = 0;
            return false;
        }

        balance = profile.BankBalance;
        return true;
    }

    /// <summary>
    /// Update the bank balance to the character's current account balance.
    /// </summary>
    private void UpdateBankBalance(EntityUid mobUid, BankAccountComponent comp)
    {
        if (TryGetBalance(mobUid, out var balance))
            comp.Balance = balance;
        else
            comp.Balance = 0;

        Dirty(mobUid, comp);
    }

    /// <summary>
    /// Component initialized - if the player exists in the entity before the BankAccountComponent, update the player's account.
    /// </summary>
    public void OnInit(EntityUid mobUid, BankAccountComponent comp, ComponentInit _)
    {
        UpdateBankBalance(mobUid, comp);
    }

    /// <summary>
    /// Player's preferences loaded (mostly for hotjoin)
    /// </summary>
    public void OnPreferencesLoaded(EntityUid mobUid, BankAccountComponent comp, PreferencesLoadedEvent _)
    {
        UpdateBankBalance(mobUid, comp);
    }

    /// <summary>
    /// Player attached, make sure the bank account is up-to-date.
    /// </summary>
    public void OnPlayerAttached(EntityUid mobUid, BankAccountComponent comp, PlayerAttachedEvent _)
    {
        UpdateBankBalance(mobUid, comp);
    }

    /// <summary>
    /// Player detached, make sure the bank account is up-to-date.
    /// </summary>
    public void OnPlayerDetached(EntityUid mobUid, BankAccountComponent comp, PlayerDetachedEvent _)
    {
        UpdateBankBalance(mobUid, comp);
    }

    /// <summary>
    /// Ensures the bank account listed in the lobby is accurate by ensuring the preferences cache is up-to-date.
    /// </summary>
    private void OnPlayerLobbyJoin(PlayerJoinedLobbyEvent args)
    {
        var cts = new CancellationToken();
        _prefsManager.RefreshPreferencesAsync(args.PlayerSession, cts);
    }
}

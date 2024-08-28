using System.Threading;
using Content.Server.Preferences.Managers;
using Content.Server.GameTicking;
using Content.Shared.Bank;
using Content.Shared.Bank.Components;
using Content.Shared.Preferences;
using Robust.Shared.Player;

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

        SubscribeLocalEvent<BankAccountComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BankAccountComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerLobbyJoin);
    }

    /// <summary>
    /// Attempts to remove money from a character's bank account. This should always be used instead of attempting to modify the bankaccountcomponent directly
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is attached to, typically the player controlled mob</param>
    /// <param name="amount">The integer amount of which to decrease the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
    public bool TryBankWithdraw(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"{amount} is invalid");
            return false;
        }

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"{mobUid} has no bank account");
            return false;
        }

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session) ||
            !_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"{mobUid} has no cached prefs");
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"{mobUid} has the wrong prefs type");
            return false;
        }

        int balance = profile.BankBalance;

        if (balance < amount)
        {
            _log.Info($"{mobUid} has insufficient funds");
            return false;
        }

        balance -= amount;

        var newProfile = profile.WithBankBalance(balance);
        var index = prefs.IndexOfCharacter(prefs.SelectedCharacter);
        _prefsManager.SetProfile(session.UserId, index, newProfile);

        bank.Balance = balance;
        Dirty(mobUid, bank);
        _log.Info($"{mobUid} withdrew {amount}");
        return true;
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
            _log.Info($"{amount} is invalid");
            return false;
        }

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"{mobUid} has no bank account");
            return false;
        }

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session) ||
            !_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            _log.Info($"{mobUid} has no cached prefs");
            return false;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            _log.Info($"{mobUid} has the wrong prefs type");
            return false;
        }

        int balance = profile.BankBalance + amount;

        var newProfile = profile.WithBankBalance(balance);
        var index = prefs.IndexOfCharacter(prefs.SelectedCharacter);
        _prefsManager.SetProfile(session.UserId, index, newProfile);

        bank.Balance = balance;
        Dirty(mobUid, bank);
        _log.Info($"{mobUid} deposited {amount}");
        return true;
    }

    /// <summary>
    /// Attempts to add money to a character's bank account. This should always be used instead of attempting to modify the bankaccountcomponent directly
    /// </summary>
    /// <param name="ent">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="balance">The amount of spesos to add into the bank account</param>
    /// <returns>true if the transaction was successful, false if it was not</returns>
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
    /// Update the bank balance to the current
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

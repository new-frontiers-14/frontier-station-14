using System.Threading;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Server.GameTicking;
using Content.Shared.Bank.Components;
using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Content.Server.Cargo.Components;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Player;

namespace Content.Server.Bank;

public sealed partial class BankSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private ISawmill _log = default!;

    private Dictionary<NetUserId, int> _cachedBankAccounts = new();

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("bank");
        SubscribeLocalEvent<BankAccountComponent, ComponentGetState>(OnBankAccountChanged);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerLobbyJoin);
        InitializeATM();
        InitializeStationATM();
    }

    // This could use a refactor into a BankAccountManager that handles your caching.
    private void OnBankAccountChanged(EntityUid mobUid, BankAccountComponent bank, ref ComponentGetState args)
    {
        var user = args.Player?.UserId;

        if (user == null || args.Player?.AttachedEntity != mobUid)
        {
            // The person reading this isn't the controller of the character.
            // Never update - return cached value if it exists, otherwise trust the data we receive.
            int balance = bank.Balance;
            if (_playerManager.TryGetSessionByEntity(mobUid, out var session) &&
                _cachedBankAccounts.ContainsKey(session.UserId))
            {
                balance = _cachedBankAccounts[session.UserId];
            }
            args.State = new BankAccountComponentState
            {
                Balance = balance
            };
            return;
        }
        var userId = user.Value;

        // Regardless of what happens, the given balance will be the returned state.
        // Possible desync with database if character is the wrong type.
        args.State = new BankAccountComponentState
        {
            Balance = bank.Balance
        };

        // Check if value is in cache.
        if (_cachedBankAccounts.ContainsKey(userId))
        {
            // Our cached value matches the request, nothing to do.
            if (_cachedBankAccounts[userId] == bank.Balance)
            {
                return;
            }
        }

        // Mismatched or missing value in cache. Update DB & cache new value.
        var prefs = _prefsManager.GetPreferences(userId);
        var character = prefs.SelectedCharacter;
        var index = prefs.IndexOfCharacter(character);

        if (character is not HumanoidCharacterProfile profile)
        {
            return;
        }

        var newProfile = profile.WithBankBalance(bank.Balance);
        _cachedBankAccounts[userId] = bank.Balance;

        _dbManager.SaveCharacterSlotAsync((NetUserId) userId, newProfile, index);
        _log.Info($"Character {profile.Name} saved");
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

        if (bank.Balance < amount)
        {
            _log.Info($"{mobUid} has insufficient funds");
            return false;
        }

        bank.Balance -= amount;
        _log.Info($"{mobUid} withdrew {amount}");
        Dirty(mobUid, bank);
        return true;
    }

    /// <summary>
    /// Attempts to add money to a character's bank account. This should always be used instead of attempting to modify the bankaccountcomponent directly
    /// </summary>
    /// <param name="mobUid">The UID that the bank account is connected to, typically the player controlled mob</param>
    /// <param name="amount">The integer amount of which to increase the bank account</param>
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

        bank.Balance += amount;
        _log.Info($"{mobUid} deposited {amount}");
        Dirty(mobUid, bank);
        return true;
    }

    /// <summary>
    /// ok so this is incredibly fucking cursed, and really shouldnt be calling LoadData
    /// However
    /// as of writing, the preferences system caches all player character data at the time of client connection.
    /// This is causing some bad bahavior where the cache is becoming outdated after character data is getting saved to the db
    /// and there is no method right now to invalidate and refresh this cache to ensure we get accurate bank data from the database,
    /// resulting in respawns/round restarts populating the bank account component with an outdated cache and then re-saving that
    /// bad cached data into the db.
    /// effectively a gigantic money exploit.
    /// So, this will have to stay cursed until I can find another way to refresh the character cache
    /// or the db gods themselves come up to smite me from below, whichever comes first
    ///
    /// EDIT 5/13/2024 THE DB GODS THEY CAME. THEY SMOTE. SAVE ME
    /// </summary>
    private void OnPlayerLobbyJoin (PlayerJoinedLobbyEvent args)
    {
        var cts = new CancellationToken();
        _prefsManager.RefreshPreferencesAsync(args.PlayerSession, cts);
    }
}

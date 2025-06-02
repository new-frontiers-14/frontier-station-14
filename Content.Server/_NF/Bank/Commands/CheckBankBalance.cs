using System.Linq;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._NF.Bank.Commands;

/// <summary>
/// Command that allows administrators to check a player's bank balance using their username.
/// Ported from Monolith.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class CheckBankBalance : IConsoleCommand
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public string Command => "checkbalance";
    public string Description => "Check a player's bank balance by username.";
    public string Help => "checkbalance <username>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Usage: checkbalance <username>");
            return;
        }

        var username = args[0];

        // First try online players
        var onlinePlayer = _playerManager.Sessions
            .FirstOrDefault(s => s.Name.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (onlinePlayer != null)
        {
            // Get the server-side BankSystem for online players
            var bankSystem = _entitySystemManager.GetEntitySystem<BankSystem>();
            if (bankSystem.TryGetBalance(onlinePlayer, out var balance))
            {
                shell.WriteLine($"Player {username} has a bank balance of {balance} credits.");
                return;
            }
        }

        // If not online, check cached preferences
        if (TryGetOfflinePlayerBalance(username, out var offlineBalance))
        {
            shell.WriteLine($"Player {username} has a bank balance of {offlineBalance} credits.");
            return;
        }

        // If not in cache, try the database
        var record = await _dbManager.GetPlayerRecordByUserName(username);
        if (record != null)
        {
            var userId = record.UserId;
            var prefs = await _dbManager.GetPlayerPreferencesAsync(userId, default);
            if (prefs != null &&
                prefs.SelectedCharacterIndex >= 0 &&
                prefs.Characters.TryGetValue(prefs.SelectedCharacterIndex, out var profile))
            {
                if (profile is HumanoidCharacterProfile humanoid)
                {
                    shell.WriteLine($"Player {username} has a bank balance of {humanoid.BankBalance} credits.");
                    return;
                }
            }
        }

        shell.WriteLine($"Could not find bank account for player {username}.");
    }

    private bool TryGetOfflinePlayerBalance(string username, out int balance)
    {
        balance = 0;

        // Check all users in the preferences cache
        foreach (var playerData in _playerManager.GetAllPlayerData())
        {
            if (_prefsManager.TryGetCachedPreferences(playerData.UserId, out var prefs))
            {
                foreach (var (_, profile) in prefs.Characters)
                {
                    if (profile is HumanoidCharacterProfile humanoid &&
                        humanoid.Name.Equals(username, StringComparison.OrdinalIgnoreCase))
                    {
                        balance = humanoid.BankBalance;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new List<string>();

            // Add online players
            options.AddRange(_playerManager.Sessions.Select(s => s.Name));

            // Add players from cached preferences
            foreach (var playerData in _playerManager.GetAllPlayerData())
            {
                if (_prefsManager.TryGetCachedPreferences(playerData.UserId, out var prefs))
                {
                    foreach (var (_, profile) in prefs.Characters)
                    {
                        if (profile is HumanoidCharacterProfile humanoid)
                        {
                            options.Add(humanoid.Name);
                        }
                    }
                }
            }

            return CompletionResult.FromHintOptions(options.Distinct(), "<username>");
        }

        return CompletionResult.Empty;
    }
}

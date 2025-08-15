using System.Linq;
using System.Text.Json.Nodes;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Content.Shared._Harmony.CCVars;
using Content.Shared._Harmony.JoinQueue; // Harmony Queue

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        /// <summary>
        ///     Used for thread safety, given <see cref="IStatusHost.OnStatusRequest"/> is called from another thread.
        /// </summary>
        private readonly object _statusShellLock = new();

        /// <summary>
        ///     Round start time in UTC, for status shell purposes.
        /// </summary>
        [ViewVariables]
        private DateTime _roundStartDateTime;

        /// <summary>
        ///     For access to CVars in status responses.
        /// </summary>
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        /// <summary>
        ///     For access to the round ID in status responses.
        /// </summary>
        [Dependency] private readonly SharedGameTicker _gameTicker = default!;

        [Dependency] private readonly IJoinQueueManager _joinQueue = default!; // Harmony Queue

        private void InitializeStatusShell()
        {
            IoCManager.Resolve<IStatusHost>().OnStatusRequest += GetStatusResponse;
        }

        private void GetStatusResponse(JsonNode jObject)
        {
            var preset = CurrentPreset ?? Preset;
            // Harmony start
            var playerCountAdminAdjustment = _cfg.GetCVar(CCVars.AdminsCountInReportedPlayerCount)
                ? 0
                : _adminManager.ActiveAdmins.Count();
            var playerCount = _joinQueue.ActualPlayersCount - playerCountAdminAdjustment;
            // Harmony end

            // This method is raised from another thread, so this better be thread safe!
            lock (_statusShellLock)
            {
                jObject["name"] = _baseServer.ServerName;
                jObject["map"] = _gameMapManager.GetSelectedMap()?.MapName;
                jObject["round_id"] = _gameTicker.RoundId;
                // Harmony start - remove queue members from the reported player count
                // jObject["players"] = _cfg.GetCVar(CCVars.AdminsCountInReportedPlayerCount)
                //     ? _playerManager.PlayerCount
                //     : _playerManager.PlayerCount - _adminManager.ActiveAdmins.Count();
                jObject["players"] = playerCount;
                // Harmony end
                jObject["soft_max_players"] = _cfg.GetCVar(CCVars.SoftMaxPlayers);
                jObject["panic_bunker"] = _cfg.GetCVar(CCVars.PanicBunkerEnabled);
                jObject["run_level"] = (int) _runLevel;
                if (preset != null)
                    jObject["preset"] = Loc.GetString(preset.ModeTitle);
                if (_runLevel >= GameRunLevel.InRound)
                {
                    jObject["round_start_time"] = _roundStartDateTime.ToString("o");
                }
            }
        }
    }
}

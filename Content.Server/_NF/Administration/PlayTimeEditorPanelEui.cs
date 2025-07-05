using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._NF.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Administration;

public sealed class PlayTimeEditorPanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminMan = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IServerDbManager _databaseMan = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeMan = default!;

    private readonly ISawmill _sawmill;

    public PlayTimeEditorPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.time_eui");
    }

    public override PlayTimeEditorPanelEuiState GetNewState()
    {
        var hasFlag = _adminMan.HasAdminFlag(Player, AdminFlags.Moderator);

        return new PlayTimeEditorPanelEuiState(hasFlag);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PlayTimeEditorEuiMessage message)
            return;

        PlaytimeTime(message.PlayerId, message.TimeData, message.Overwrite);
    }

    public async void PlaytimeTime(string playerId, List<PlayTimeEditorData> timeData, bool overwrite)
    {
        if (!_adminMan.HasAdminFlag(Player, AdminFlags.Moderator))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to add roles time without moderator flag)");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-no-perms"), Color.Red));
            return;
        }

        var playerData = await _playerLocator.LookupIdByNameAsync(playerId);
        if (playerData == null)
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to add roles time to not existing player {playerId})");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-no-player-database-message"), Color.Red));
            return;
        }

        if (overwrite)
            SetTime(playerData.UserId, timeData);
        else
            AddTime(playerData.UserId, timeData);
    }

    private bool ValidateTimeData(List<PlayTimeEditorData> timeData)
    {
        foreach (var data in timeData)
        {
            try
            {
                var minutes = PlayTimeCommandUtilities.CountMinutes(data.TimeString);
                
                // Check for invalid or overflow values
                if (double.IsNaN(minutes) || double.IsInfinity(minutes))
                {
                    _sawmill.Warning($"{Player.Name} ({Player.UserId}) provided invalid time value: {data.TimeString}");
                    SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-invalid-time"), Color.Red));
                    return false;
                }
                
                // Check for overflow before creating TimeSpan
                if (minutes > TimeSpan.MaxValue.TotalMinutes || minutes < TimeSpan.MinValue.TotalMinutes)
                {
                    _sawmill.Warning($"{Player.Name} ({Player.UserId}) provided time value that would overflow: {data.TimeString} ({minutes} minutes)");
                    SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
                    return false;
                }
                
                // Additional check for extremely large values that could cause issues
                if (minutes > 525600 * 1000) // More than 1000 years
                {
                    _sawmill.Warning($"{Player.Name} ({Player.UserId}) provided unreasonably large time value: {data.TimeString} ({minutes} minutes)");
                    SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
                    return false;
                }
            }
            catch (OverflowException ex)
            {
                _sawmill.Warning($"{Player.Name} ({Player.UserId}) provided time value that would overflow: {data.TimeString} - {ex.Message}");
                SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
                return false;
            }
            catch (ArgumentException ex)
            {
                _sawmill.Warning($"{Player.Name} ({Player.UserId}) provided invalid time format: {data.TimeString} - {ex.Message}");
                SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-invalid-time"), Color.Red));
                return false;
            }
            catch (Exception ex)
            {
                _sawmill.Error($"{Player.Name} ({Player.UserId}) error parsing time string '{data.TimeString}': {ex}");
                SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-invalid-time"), Color.Red));
                return false;
            }
        }
        return true;
    }

    public async void SetTime(NetUserId userId, List<PlayTimeEditorData> timeData)
    {
        if (!ValidateTimeData(timeData))
            return;

        var updateList = new List<PlayTimeUpdate>();

        try
        {
            foreach (var data in timeData)
            {
                var minutes = PlayTimeCommandUtilities.CountMinutes(data.TimeString);
                var time = TimeSpan.FromMinutes(minutes);
                updateList.Add(new PlayTimeUpdate(userId, data.PlaytimeTracker, time));
            }

            await _databaseMan.UpdatePlayTimes(updateList);

            _sawmill.Info($"{Player.Name} ({Player.UserId}) saved {updateList.Count} trackers for {userId}");

            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-set-success"), Color.LightGreen));
        }
        catch (OverflowException ex)
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) attempted to set playtime with overflow values: {ex}");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
        }
        catch (Exception ex)
        {
            _sawmill.Error($"{Player.Name} ({Player.UserId}) encountered error setting playtime: {ex}");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-error"), Color.Red));
        }
    }

    public async void AddTime(NetUserId userId, List<PlayTimeEditorData> timeData)
    {
        if (!ValidateTimeData(timeData))
            return;

        try
        {
            var playTimeList = await _databaseMan.GetPlayTimes(userId);

            Dictionary<string, TimeSpan> playTimeDict = new();

            foreach (var playTime in playTimeList)
            {
                playTimeDict.Add(playTime.Tracker, playTime.TimeSpent);
            }

            var updateList = new List<PlayTimeUpdate>();

            foreach (var data in timeData)
            {
                var minutes = PlayTimeCommandUtilities.CountMinutes(data.TimeString);
                var time = TimeSpan.FromMinutes(minutes);
                
                if (playTimeDict.TryGetValue(data.PlaytimeTracker, out var existingTime))
                {
                    // Check for overflow when adding existing time
                    try
                    {
                        // Use checked context to catch arithmetic overflow
                        var newTicks = checked(time.Ticks + existingTime.Ticks);
                        
                        // Additional safety check
                        if (newTicks > TimeSpan.MaxValue.Ticks || newTicks < TimeSpan.MinValue.Ticks)
                        {
                            _sawmill.Warning($"{Player.Name} ({Player.UserId}) attempted to add time that would overflow for tracker {data.PlaytimeTracker}");
                            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
                            return;
                        }
                        
                        time = new TimeSpan(newTicks);
                    }
                    catch (OverflowException)
                    {
                        _sawmill.Warning($"{Player.Name} ({Player.UserId}) attempted to add time that would overflow for tracker {data.PlaytimeTracker}");
                        SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
                        return;
                    }
                }

                updateList.Add(new PlayTimeUpdate(userId, data.PlaytimeTracker, time));
            }

            await _databaseMan.UpdatePlayTimes(updateList);

            _sawmill.Info($"{Player.Name} ({Player.UserId}) saved {updateList.Count} trackers for {userId}");

            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-add-success"), Color.LightGreen));
        }
        catch (OverflowException ex)
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) attempted to add playtime with overflow values: {ex}");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-overflow"), Color.Red));
        }
        catch (Exception ex)
        {
            _sawmill.Error($"{Player.Name} ({Player.UserId}) encountered error adding playtime: {ex}");
            SendMessage(new PlayTimeEditorWarningEuiMessage(Loc.GetString("playtime-editor-panel-warning-error"), Color.Red));
        }
    }

    public override async void Opened()
    {
        base.Opened();
        _adminMan.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _adminMan.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
        {
            return;
        }

        StateDirty();
    }
}

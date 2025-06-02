using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Objectives;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.CustomObjectiveSummary;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._DV.CustomObjectiveSummary;

public sealed class CustomObjectiveSummarySystem : EntitySystem
{
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    // [Dependency] private readonly SharedFeedbackOverwatchSystem _feedback = default!; // Frontier
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Frontier
    [Dependency] private readonly ObjectivesSystem _objectives = default!; // Frontier

    private int _maxLengthSummaryLength; // Frontier: moved from ObjectiveSystem
    private Dictionary<NetUserId, PlayerStory> _stories = new(); // Frontier: store one story per user per round

    public override void Initialize()
    {
        SubscribeLocalEvent<EvacShuttleLeftEvent>(OnEvacShuttleLeft);
        // SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd); // Frontier
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestarted); // Frontier

        _net.RegisterNetMessage<CustomObjectiveClientSetObjective>(OnCustomObjectiveFeedback);

        Subs.CVar(_cfg, DCCVars.MaxObjectiveSummaryLength, len => _maxLengthSummaryLength = len, true); // Frontier: moved from ObjectiveSystem
    }

    private void OnCustomObjectiveFeedback(CustomObjectiveClientSetObjective msg)
    {
        if (!_mind.TryGetMind(msg.MsgChannel.UserId, out var mind) || mind is not { } mindEnt)
            return;

        if (mind.Value.Comp.Objectives.Count == 0)
            return;

        var characterName = _objectives.GetTitle((mindEnt, mindEnt.Comp), mindEnt.Comp.CharacterName ?? Loc.GetString("custom-objective-unknown-name"));
        if (_stories.TryGetValue(msg.MsgChannel.UserId, out var story))
        {
            story.CharacterName = characterName;
            story.Story = msg.Summary;
        }
        else
        {
            _stories[msg.MsgChannel.UserId] = new PlayerStory(characterName, msg.Summary);
        }

        // Ensure that the current mind has their summary setup (so they can come back to it if disconnected)
        var comp = EnsureComp<CustomObjectiveSummaryComponent>(mind.Value);

        comp.ObjectiveSummary = msg.Summary;
        Dirty(mind.Value.Owner, comp);

        _adminLog.Add(LogType.ObjectiveSummary, $"{ToPrettyString(mind.Value.Comp.OwnedEntity)} wrote objective summary: {msg.Summary}");
    }

    private void OnEvacShuttleLeft(EvacShuttleLeftEvent args)
    {
        var allMinds = _mind.GetAliveHumans();

        foreach (var mind in allMinds)
        {
            // Only send the popup to people with objectives.
            if (mind.Comp.Objectives.Count == 0)
                continue;

            if (!_player.TryGetSessionById(mind.Comp.UserId, out var session))
                continue;

            RaiseNetworkEvent(new CustomObjectiveSummaryOpenMessage(), session);
        }
    }

    // Frontier: unneeded
    /*
    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        var allMinds = _mind.GetAliveHumans();

        foreach (var mind in allMinds)
        {
            if (mind.Comp.Objectives.Count == 0)
                continue;

            _feedback.SendPopupMind(mind, "RemoveGreentextPopup");
        }
    }
    */
    // End Frontier: unneeded

    // Frontier: custom objective text
    public string GetCustomObjectiveText()
    {
        StringBuilder objectiveText = new();

        foreach (var story in _stories.Values)
        {
            story.Story.Trim();
            if (story.Story.Length > _maxLengthSummaryLength)
                story.Story = story.Story.Substring(0, _maxLengthSummaryLength);

            objectiveText.AppendLine(Loc.GetString("custom-objective-intro", ("title", story.CharacterName)));
            objectiveText.AppendLine(Loc.GetString("custom-objective-format", ("line", FormattedMessage.EscapeText(story.Story))));
            objectiveText.AppendLine("");
        }
        return objectiveText.ToString();
    }

    private void OnRoundRestarted(RoundRestartCleanupEvent args)
    {
        _stories.Clear();
    }

    sealed class PlayerStory(string characterName, string story)
    {
        public string CharacterName = characterName;
        public string Story = story;
    }
    // End Frontier
}

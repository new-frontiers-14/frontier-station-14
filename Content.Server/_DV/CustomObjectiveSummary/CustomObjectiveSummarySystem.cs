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

    public override void Initialize()
    {
        SubscribeLocalEvent<EvacShuttleLeftEvent>(OnEvacShuttleLeft);
        // SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd); // Frontier

        _net.RegisterNetMessage<CustomObjectiveClientSetObjective>(OnCustomObjectiveFeedback);

        Subs.CVar(_cfg, DCCVars.MaxObjectiveSummaryLength, len => _maxLengthSummaryLength = len, true); // Frontier: moved from ObjectiveSystem
    }

    private void OnCustomObjectiveFeedback(CustomObjectiveClientSetObjective msg)
    {
        if (!_mind.TryGetMind(msg.MsgChannel.UserId, out var mind))
            return;

        if (mind.Value.Comp.Objectives.Count == 0)
            return;

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
        var allMinds = _mind.GetAliveHumans();

        StringBuilder objectiveText = new();

        foreach (var mind in allMinds)
        {
            if (TryComp<CustomObjectiveSummaryComponent>(mind, out var customComp) &&
                customComp.ObjectiveSummary.Length > 0)
            {
                customComp.ObjectiveSummary.Trim();
                if (customComp.ObjectiveSummary.Length > _maxLengthSummaryLength)
                    customComp.ObjectiveSummary = customComp.ObjectiveSummary.Substring(0, _maxLengthSummaryLength);

                var title = _objectives.GetTitle((mind, mind.Comp), mind.Comp.CharacterName ?? Loc.GetString("custom-objective-unknown-name"));
                objectiveText.AppendLine(Loc.GetString("custom-objective-intro", ("title", title)));

                // // We have to spit it like this to make it readable. Yeah, it sucks but for some reason the entire thing
                // // is just one long string...
                // var words = customComp.ObjectiveSummary.Split(" ");
                // var currentLine = "";
                // foreach (var word in words)
                // {
                //     currentLine += word + " ";

                //     // magic number
                //     if (currentLine.Length <= 50)
                //         continue;

                //     objectiveText.AppendLine(Loc.GetString("custom-objective-format", ("line", currentLine)));
                //     currentLine = "";
                // }
                objectiveText.AppendLine(Loc.GetString("custom-objective-format", ("line", FormattedMessage.EscapeText(customComp.ObjectiveSummary))));

                objectiveText.AppendLine("");
            }
        }
        return objectiveText.ToString();
    }
    // End Frontier
}

﻿using Content.Server.GameTicking;
﻿using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives;

public sealed class ObjectivesSystem : SharedObjectivesSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    /// <summary>
    /// Adds objective text for each game rule's players on round end.
    /// </summary>
    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        // go through each gamerule getting data for the roundend summary.
        var summaries = new Dictionary<string, Dictionary<string, List<EntityUid>>>();
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var info = new ObjectivesTextGetInfoEvent(new List<EntityUid>(), string.Empty);
            RaiseLocalEvent(uid, ref info);
            if (info.Minds.Count == 0)
                continue;

            // first group the gamerules by their agents, for example 2 different dragons
            var agent = info.AgentName;
            if (!summaries.ContainsKey(agent))
                summaries[agent] = new Dictionary<string, List<EntityUid>>();

            var prepend = new ObjectivesTextPrependEvent("");
            RaiseLocalEvent(uid, ref prepend);

            // next group them by their prepended texts
            // for example with traitor rule, group them by the codewords they share
            var summary = summaries[agent];
            if (summary.ContainsKey(prepend.Text))
            {
                // same prepended text (usually empty) so combine them
                summary[prepend.Text].AddRange(info.Minds);
            }
            else
            {
                summary[prepend.Text] = info.Minds;
            }
        }

        // convert the data into summary text
        foreach (var (agent, summary) in summaries)
        {
            // first get the total number of players that were in these game rules combined
            var total = 0;
            foreach (var (_, minds) in summary)
            {
                total += minds.Count;
            }

            var result = Loc.GetString("objectives-round-end-result", ("count", total), ("agent", agent));
            // next add all the players with its own prepended text
            foreach (var (prepend, minds) in summary)
            {
                if (prepend != string.Empty)
                    result += prepend;

                // add space between the start text and player list
                result += "\n";

                AddSummary(ref result, agent, minds);
            }

            ev.AddLine(result + "\n");
        }
    }

    private void AddSummary(ref string result, string agent, List<EntityUid> minds)
    {
        foreach (var mindId in minds)
        {
            if (!TryComp(mindId, out MindComponent? mind))
                continue;

            var name = mind.CharacterName;
            _mind.TryGetSession(mindId, out var session);
            var username = session?.Name;

            string title;
            if (username != null)
            {
                if (name != null)
                    title = Loc.GetString("objectives-player-user-named", ("user", username), ("name", name));
                else
                    title = Loc.GetString("objectives-player-user", ("user", username));
            }
            else
            {
                // nothing to identify the player by, just give up
                if (name == null)
                    continue;

                title = Loc.GetString("objectives-player-named", ("name", name));
            }

            result += "\n";

            var objectives = mind.AllObjectives.ToArray();
            if (objectives.Length == 0)
            {
                result += Loc.GetString("objectives-no-objectives", ("title", title), ("agent", agent));
                continue;
            }

            result += Loc.GetString("objectives-with-objectives", ("title", title), ("agent", agent));

            foreach (var objectiveGroup in objectives.GroupBy(o => Comp<ObjectiveComponent>(o).Issuer))
            {
                result += "\n" + Loc.GetString($"objective-issuer-{objectiveGroup.Key}");

                foreach (var objective in objectiveGroup)
                {
                    var info = GetInfo(objective, mindId, mind);
                    if (info == null)
                        continue;

                    var objectiveTitle = info.Value.Title;
                    var progress = info.Value.Progress;
                    if (progress > 0.99f)
                    {
                        result += "\n- " + Loc.GetString(
                            "objectives-objective-success",
                            ("objective", objectiveTitle),
                            ("markupColor", "green")
                        );
                    }
                    else
                    {
                        result += "\n- " + Loc.GetString(
                            "objectives-objective-fail",
                            ("objective", objectiveTitle),
                            ("progress", (int) (progress * 100)),
                            ("markupColor", "red")
                        );
                    }
                }
            }
        }
    }

    public EntityUid? GetRandomObjective(EntityUid mindId, MindComponent mind, string objectiveGroupProto)
    {
        if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(objectiveGroupProto, out var groups))
        {
            Log.Error($"Tried to get a random objective, but can't index WeightedRandomPrototype {objectiveGroupProto}");
            return null;
        }

        // TODO replace whatever the fuck this is with a proper objective selection system
        // yeah the old 'preventing infinite loops' thing wasn't super elegant either and it mislead people on what exactly it did
        var tries = 0;
        while (tries < 20)
        {
            var groupName = groups.Pick(_random);

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(groupName, out var group))
            {
                Log.Error($"Couldn't index objective group prototype {groupName}");
                return null;
            }

            var proto = group.Pick(_random);
            var objective = TryCreateObjective(mindId, mind, proto);
            if (objective != null)
                return objective;

            tries++;
        }

        return null;
    }
}

/// <summary>
/// Raised on the game rule to get info for any objectives.
/// If its minds list is set then the players will have their objectives shown in the round end text.
/// AgentName is the generic name for a player in the list.
/// </summary>
/// <remarks>
/// The objectives system already checks if the game rule is added so you don't need to check that in this event's handler.
/// </remarks>
[ByRefEvent]
public record struct ObjectivesTextGetInfoEvent(List<EntityUid> Minds, string AgentName);

/// <summary>
/// Raised on the game rule before text for each agent's objectives is added, letting you prepend something.
/// </summary>
[ByRefEvent]
public record struct ObjectivesTextPrependEvent(string Text);

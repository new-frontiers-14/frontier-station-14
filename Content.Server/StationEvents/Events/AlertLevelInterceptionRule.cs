﻿using Content.Server.GameTicking.Components;
using Content.Server.StationEvents.Components;
using Content.Server.AlertLevel;

namespace Content.Server.StationEvents.Events;

public sealed class AlertLevelInterceptionRule : StationEventSystem<AlertLevelInterceptionRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;

    protected override void Started(EntityUid uid, AlertLevelInterceptionRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;
        if (_alertLevelSystem.GetLevel(chosenStation.Value) != "green")
            return;

        _alertLevelSystem.SetLevel(chosenStation.Value, component.AlertLevel, true, true, true);
    }
}
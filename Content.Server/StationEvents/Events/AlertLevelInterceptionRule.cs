using Content.Server.StationEvents.Components;
using Content.Server.AlertLevel;
﻿using Content.Shared.GameTicking.Components;

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
        // Frontier - note: levels are globally set/gotten, regardless of arg
        if (_alertLevelSystem.GetLevel(chosenStation.Value) != "green")
            return;

        _alertLevelSystem.SetLevel(chosenStation.Value, component.AlertLevel, true, true, true);
    }
}

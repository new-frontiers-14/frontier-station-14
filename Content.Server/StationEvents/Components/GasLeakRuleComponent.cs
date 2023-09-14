﻿using Content.Server.StationEvents.Events;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GasLeakRule))]
public sealed partial class GasLeakRuleComponent : Component
{
    public readonly Gas[] LeakableGases =
    {
        Gas.Miasma,
        Gas.Plasma,
        Gas.Tritium,
        Gas.Frezon,
    };

    /// <summary>
    ///     Running cooldown of how much time until another leak.
    /// </summary>
    public float TimeUntilLeak;

    /// <summary>
    ///     How long between more gas being added to the tile.
    /// </summary>
    public float LeakCooldown = 1.0f;

    // Event variables
    public EntityUid TargetStation;
    public EntityUid TargetGrid;
    public Vector2i TargetTile;
    public EntityCoordinates TargetCoords;
    public bool FoundTile;
    public Gas LeakGas;
    public float MolesPerSecond;
    public readonly int MinimumMolesPerSecond = 20;

    /// <summary>
    ///     Don't want to make it too fast to give people time to flee.
    /// </summary>
    public int MaximumMolesPerSecond = 50;

    public int MinimumGas = 250;
    public int MaximumGas = 1000;
    public float SparkChance = 0.05f;
}

<<<<<<< HEAD
#nullable enable
=======
﻿namespace Content.IntegrationTests;
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8

/// <inheritdoc/>
public sealed class PoolSettings : PairSettings
{
    public override bool Connected
    {
        get => _connected || InLobby;
        init => _connected = value;
    }

    private readonly bool _dummyTicker = true;
    private readonly bool _connected;

    /// <summary>
    /// Set to true if the given server should be using a dummy ticker. Ignored if <see cref="InLobby"/> is true.
    /// </summary>
    public bool DummyTicker
    {
        get => _dummyTicker && !InLobby;
        init => _dummyTicker = value;
    }

    /// <summary>
    /// If true, this enables the creation of admin logs during the test.
    /// </summary>
    public bool AdminLogsEnabled { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be in the lobby.
    /// If the pair is not in the lobby at the end of the test, this test must be marked as dirty.
    /// </summary>
    /// <remarks>
    /// If this is enabled, the value of <see cref="DummyTicker"/> is ignored.
    /// </remarks>
    public bool InLobby { get; init; }

    /// <summary>
    /// Set this to true to skip loading the content files.
    /// Note: This setting won't work with a client.
    /// </summary>
    public bool NoLoadContent { get; init; }

    /// <summary>
    /// Set this to the path of a map to have the given server/client pair load the map.
    /// </summary>
    public string Map { get; init; } = PoolManager.TestMap;

<<<<<<< HEAD
    /// <summary>
    /// Overrides the test name detection, and uses this in the test history instead
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    /// If set, this will be used to call <see cref="IRobustRandom.SetSeed"/>
    /// </summary>
    public int? ServerSeed { get; set; }

    /// <summary>
    /// If set, this will be used to call <see cref="IRobustRandom.SetSeed"/>
    /// </summary>
    public int? ClientSeed { get; set; }

    /// <summary>
    /// Frontier: the preset to run the game in.
    /// Set to secret for upstream tests to mimic upstream behaviour.
    /// If you need to check adventure game rule things, set this to nfadventure or nfpirate.
    /// </summary>
    public string GameLobbyDefaultPreset { get; set; } = "secret";

    #region Inferred Properties

    /// <summary>
    /// If the returned pair must not be reused
    /// </summary>
    public bool MustNotBeReused => Destructive || NoLoadContent || NoLoadTestPrototypes;

    /// <summary>
    /// If the given pair must be brand new
    /// </summary>
    public bool MustBeNew => Fresh || NoLoadContent || NoLoadTestPrototypes;

    public bool UseDummyTicker => !InLobby && DummyTicker;

    public bool ShouldBeConnected => InLobby || Connected;

    #endregion

    /// <summary>
    /// Tries to guess if we can skip recycling the server/client pair.
    /// </summary>
    /// <param name="nextSettings">The next set of settings the old pair will be set to</param>
    /// <returns>If we can skip cleaning it up</returns>
    public bool CanFastRecycle(PoolSettings nextSettings)
=======
    public override bool CanFastRecycle(PairSettings nextSettings)
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
    {
        if (!base.CanFastRecycle(nextSettings))
            return false;

        if (nextSettings is not PoolSettings next)
            return false;

        // Check that certain settings match.
<<<<<<< HEAD
        return !ShouldBeConnected == !nextSettings.ShouldBeConnected
               && UseDummyTicker == nextSettings.UseDummyTicker
               && Map == nextSettings.Map
               && InLobby == nextSettings.InLobby
               && GameLobbyDefaultPreset == nextSettings.GameLobbyDefaultPreset; // Frontier: swappable presets
=======
        return DummyTicker == next.DummyTicker
               && Map == next.Map
               && InLobby == next.InLobby;
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
    }
}

// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0


using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

#region Reactor Caps
/// <summary>
/// Appearance key for the reactor caps.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorCapVisuals
{
    Sprite
}
#endregion

#region Reactor
/// <summary>
/// Appearance keys for the reactor.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorVisuals
{
    Sprite,
    Status,
    Input,
    Output,
    Lights,
    Smoke,
    Fire,
}

/// <summary>
/// Visual sprite layers for the reactor.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorVisualLayers
{
    Sprite,
    Status,
    Input,
    Output,
    Lights,
    Smoke,
    Fire,
}

/// <summary>
/// Reactor sprites.
/// </summary>
[Serializable, NetSerializable]
public enum Reactors
{
    Normal,
    Melted,
}

/// <summary>
/// Status screens.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorStatusLights
{
    Off,
    Active,
    Overheat,
    Meltdown,
}

/// <summary>
/// Warning lights settings.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorWarningLights
{
    LightsOff,
    LightsWarning,
    LightsMeltdown,
}
#endregion

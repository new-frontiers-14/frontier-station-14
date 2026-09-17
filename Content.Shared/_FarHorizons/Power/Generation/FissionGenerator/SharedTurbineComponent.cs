// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0


using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

[Serializable, NetSerializable]
public enum TurbineUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TurbineBuiState : BoundUserInterfaceState
{
    // Indicator Lights
    public bool Overspeed;
    public bool Stalling;
    public bool Overtemp;
    public bool Undertemp;

    // Speed
    public float RPM;
    public float BestRPM;

    // Flow rate
    public float FlowRateMin;
    public float FlowRateMax;
    public float FlowRate;

    // Stator load
    public float StatorLoadMin;
    public float StatorLoad;

    // Power generation
    public float PowerGeneration;
    public float PowerSupply;

    // Health
    public float Health;
    public float HealthMax;

    // Parts
    public NetEntity? Blade;
    public NetEntity? Stator;
}

[Serializable, NetSerializable]
public sealed class TurbineChangeFlowRateMessage(float flowRate) : BoundUserInterfaceMessage
{
    public float FlowRate { get; } = flowRate;
}

[Serializable, NetSerializable]
public sealed class TurbineChangeStatorLoadMessage(float statorLoad) : BoundUserInterfaceMessage
{
    public float StatorLoad { get; } = statorLoad;
}

// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 Milon
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared._NF.Atmos.Systems;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, Access(typeof(SharedGasDepositSystem))]
public sealed partial class GasDepositComponent : Component
{
    /// <summary>
    /// Gas composition in this deposit.
    /// Gas amounts should add up to 1.
    /// </summary>
    [DataField]
    public GasMixture Composition = new();

    /// <summary>
    /// Total moles of gas left in this deposit, for non-deep deposits.
    /// </summary>
    [DataField]
    public float GasLeft = 0f;

    /// <summary>
    /// Yield, for deep deposits.
    /// </summary>
    [DataField]
    public float Yield = 1f;

    /// <summary>
    /// How much to drop yield per second per mol/s base extraction rate.
    /// </summary>
    [DataField]
    public float YieldDrop = 1f / 100f / 350f; // drop by 1% per 350mol extracted at 100% yield

    /// <summary>
    /// Minimum yield to never drop below.
    /// </summary>
    [DataField]
    public float MinYield = 0.25f;

    /// <summary>
    /// Whether this deposit is yield-based.
    /// Whether to operate based on yield rather than a fixed amount of moles.
    /// </summary>
    [DataField]
    public bool YieldBased = false;

    /// <summary>
    /// Temperature of the output gas mixture, in K.
    /// </summary>
    [DataField]
    public float OutputTemperature = Atmospherics.T20C;

    /// <summary>
    /// The maximum number of moles for this deposit to be considered "mostly depleted".
    /// </summary>
    [ViewVariables]
    public float LowMoles;
}

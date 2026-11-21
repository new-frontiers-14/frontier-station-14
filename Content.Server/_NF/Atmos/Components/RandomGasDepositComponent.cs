// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 Milon
// SPDX-FileCopyrightText: 2025 Whatstone
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._NF.Atmos.Systems;
using Content.Shared._NF.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, Access(typeof(GasDepositSystem))]
public sealed partial class RandomGasDepositComponent : Component
{
    /// <summary>
    /// The name of the prototype used to populate the gas deposit in this entity.
    /// If null or invalid, will be selected from existing set at random.
    /// </summary>
    [DataField]
    public ProtoId<GasDepositPrototype>? DepositPrototype;

    /// <summary>
    /// A scale factor on the deposit's size.
    /// After each gas is chosen from the deposit prototype, the scale factor is multiplied into the deposit size.
    /// Mono - Is used as yield for deep deposits. Minimum yield is also multiplied by the square root of this.
    /// </summary>
    [DataField]
    public float Scale = 1.0f;
}

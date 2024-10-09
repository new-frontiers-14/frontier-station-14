using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Server._NF.Medical.Components;

[RegisterComponent]
public sealed partial class SectorMedicalBountyValueComponent : Component
{
    /// <summary>
    /// A collection of rates per damage type. Values
    /// </summary>
    [DataField]
    DamageSpecifier PenaltyPerUnit;
}

[Serializable, NetSerializable]
public struct DamageRates
{
    [DataField]
    float Blunt;
    [DataField]
    float Pierce;
    [DataField]
    float Slash;
    [DataField]
    float Heat;
    [DataField]
    float Cold;
    [DataField]
    float Shock;
    [DataField]
    float Caustic;
    [DataField]
    float Poison;
    [DataField]
    float Radiation;
    [DataField]
    float Asphyxiation;
    [DataField]
    float Bloodloss;
    [DataField]
    float Genetic;
}
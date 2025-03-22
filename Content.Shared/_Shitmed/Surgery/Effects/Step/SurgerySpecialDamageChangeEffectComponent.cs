using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._Shitmed.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgerySpecialDamageChangeEffectComponent : Component
{
    [DataField]
    public string DamageType = "";

    [DataField]
    public bool IsConsumable;
}
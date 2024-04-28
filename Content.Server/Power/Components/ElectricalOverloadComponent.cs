using Content.Shared.Explosion;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class ElectricalOverloadComponent : Component
{
    [ValidatePrototypeId<ExplosionPrototype>]
    [DataField]
    public string ExplosionOnOverload = "Default";

    [ViewVariables]
    public DateTime ExplodeAt = DateTime.MaxValue;

    [ViewVariables]
    public DateTime NextBuzz = DateTime.MaxValue;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Corvax.Penetration;

[RegisterComponent, NetworkedComponent]
public sealed partial class PenetratableComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int StoppingPower;
}

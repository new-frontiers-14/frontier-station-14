using Robust.Shared.GameStates;

namespace Content.Shared._UNEON.VipUplink;

[RegisterComponent, NetworkedComponent]
public sealed partial class VipUplinkCartridgeComponent : Component;

/// <summary>Presentation only. Purchases always consult the server subscription cache again.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VipUplinkAccessComponent : Component
{
    [DataField, AutoNetworkedField] public int Tier;
}

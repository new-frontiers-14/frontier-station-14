namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(NF_ShipConnectCartridgeSystem))]
public sealed partial class NF_ShipConnectCartridgeComponent : Component
{
    /// <summary>
    /// Station entity keeping track of logistics stats
    /// </summary>
    [DataField]
    public EntityUid? Station;
}

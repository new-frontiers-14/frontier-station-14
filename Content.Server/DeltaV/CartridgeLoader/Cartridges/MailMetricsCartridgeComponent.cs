namespace Content.Server.DeltaV.CartridgeLoader.Cartridges;

// Frontier: removed the EntityUid of the station from the component 
[RegisterComponent, Access(typeof(MailMetricsCartridgeSystem))]
public sealed partial class MailMetricsCartridgeComponent : Component
{
}

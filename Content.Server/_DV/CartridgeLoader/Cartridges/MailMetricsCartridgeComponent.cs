namespace Content.Server._DV.CartridgeLoader.Cartridges;

// Frontier: removed the EntityUid of the station from the component 
[RegisterComponent, Access(typeof(MailMetricsCartridgeSystem))]
public sealed partial class MailMetricsCartridgeComponent : Component
{
}

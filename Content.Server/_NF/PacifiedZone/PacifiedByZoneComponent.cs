namespace Content.Server._NF.PacifiedZone
{
    // Denotes an entity as being pacified by a zone.
    // An entity with PacifiedComponent but not PacifiedByZoneComponent is naturally pacified
    // (e.g. through Pax, or the Pious trait)
    [RegisterComponent]
    public sealed partial class PacifiedByZoneComponent : Component
    {
    }
}
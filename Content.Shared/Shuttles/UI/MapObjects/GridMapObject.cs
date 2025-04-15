using Content.Shared.Shuttles.Components;

namespace Content.Shared.Shuttles.UI.MapObjects;

public record struct GridMapObject : IMapObject
{
    public string Name { get; set; }

    // Frontier: Service flags
    public ServiceFlags ServiceFlags { get; set; }
    public bool HideButton { get; init; }
    public EntityUid Entity;
}

using Content.Shared.MassMedia.Systems;

namespace Content.Shared.MassMedia.Components;

[RegisterComponent]
public sealed partial class SectorNewsComponent : Component
{
    public static List<NewsArticle> Articles = new();
}

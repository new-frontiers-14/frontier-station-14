using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared._NF.CCVar;
using Content.Shared.Research.Components;
using Robust.Shared.Configuration; // Frontier

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!; // Frontier

    private void InitializeSource()
    {
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!CanProduce(source))
            return;
        // Frontier - Add points modifier
        var cvarModifier = _configuration.GetCVar(NFCCVars.SciencePointGainModifier);
        args.Points += (int) (source.Comp.PointsPerSecond * cvarModifier);
    }

    public bool CanProduce(Entity<ResearchPointSourceComponent> source)
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }
}

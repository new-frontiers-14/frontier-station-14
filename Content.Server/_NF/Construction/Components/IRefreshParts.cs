using Content.Shared.Construction.Components;

namespace Content.Server._NF.Construction.Components
{
    [RequiresExplicitImplementation]
    public interface IRefreshParts
    {
        void RefreshParts(IEnumerable<MachinePartComponent> parts);
    }
}

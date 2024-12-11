using Content.Shared.Shuttles.Components;
using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [Access(typeof(AdvDockingSystem))]
    public sealed partial class AdvDockingComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        public bool IsOn;
    }
}

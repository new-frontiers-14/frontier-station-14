using Content.Shared.Shuttles.Components;
using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [Access(typeof(AdvDockingSystem))]
    public sealed partial class AdvDockingComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool IsOn { get; set; } = true;

        /// <summary>
        ///     Frontier - Amount of charge this needs from an APC per second to function.
        /// </summary>
        public float OriginalLoad { get; set; } = 0;
    }
}

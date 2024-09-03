using Content.Shared.Tools;
using Robust.Shared.Utility;

namespace Content.Server._NF.Tools.Components
{
    [RegisterComponent]
    public sealed partial class OnToolsUseComponent : Component
    {
        /// <summary>
        ///     Disable the use of tools on the entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public PrototypeFlags<ToolQualityPrototype> DisabledQualities = new(["Anchoring", "Prying", "Screwing", "Cutting", "Welding", "Pulsing", "Slicing", "Sawing", "Honking", "Rolling", "Digging"]);
    }
}

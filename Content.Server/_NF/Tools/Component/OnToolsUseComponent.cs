using Content.Shared.Tools;
using Robust.Shared.Utility;

namespace Content.Server._NF.Tools.Components
{
    [RegisterComponent]
    public sealed partial class OnToolsUseComponent : Component
    {
        /// <summary>
        ///     Disables the use of all tools on an entity
        /// </summary>
        [DataField("disabled"), ViewVariables(VVAccess.ReadWrite)]
        public bool AllToolUseDisabled;

        /// <summary>
        ///     Disable the use of tools on the entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public PrototypeFlags<ToolQualityPrototype> DisabledQualities = [];
    }
}

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Tools.Components
{
    [RegisterComponent]
    public sealed partial class OnToolsUseComponent : Component
    {
        /// <summary>
        ///     Disable the use of tools on the entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Disabled;
    }
}

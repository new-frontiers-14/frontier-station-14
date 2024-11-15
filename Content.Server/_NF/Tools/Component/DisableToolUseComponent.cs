using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Tools.Components
{
    [RegisterComponent]
    public sealed partial class DisableToolUseComponent : Component
    {
        // A field for each tool use type to allow for inheritance
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Anchoring;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Prying;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Screwing;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Cutting;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Welding;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Pulsing;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Slicing;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Sawing;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Honking;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Rolling;
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Digging;
    }
}

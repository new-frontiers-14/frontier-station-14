using Content.Server._NF.Chemistry.EntitySystems;
using Content.Shared._NF.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemPrenticeSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemPrenticeSystem))]
    public sealed partial class ChemPrenticeComponent : Component
    {
        [DataField("pillType"), ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}

using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemMasterSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemMasterSystem))]
    public sealed partial class ChemMasterComponent : Component
    {
        [DataField("pillType"), ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        // Frontier: upgrade fields
        [DataField]
        public uint[] PillDosageLimitPerTier;

        [DataField]
        public ProtoId<MachinePartPrototype> PillDosageMachinePart = "Manipulator";

        [ViewVariables]
        public uint MaxVolume;

        [DataField]
        public uint[] MaxVolumePerTier;

        [DataField]
        public ProtoId<MachinePartPrototype> MaxVolumeMachinePart = "MatterBin";
        // End Frontier
    }
}

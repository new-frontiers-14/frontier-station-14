using Content.Server._NF.Chemistry.Systems;
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server._NF.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemPrenticeSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemPrenticeSystem))]
    public sealed partial class ChemPrenticeComponent : Component
    {
        [DataField]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}

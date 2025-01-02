using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>

    /*[Serializable, NetSerializable]
    public sealed class ChemPrenticeCreatePillsMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly uint Number;
        public readonly string Label;

        public ChemPrenticeCreatePillsMessage(uint dosage, uint number, string label)
        {
            Dosage = dosage;
            Number = number;
            Label = label;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeOutputToBottleMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly string Label;

        public ChemPrenticeOutputToBottleMessage(uint dosage, string label)
        {
            Dosage = dosage;
            Label = label;
        }
    }

    public enum ChemPrenticeMode
    {
        Transfer,
        Discard,
    }

    public static class ChemPrenticeReagentAmountToFixedPoint
    {
        public static FixedPoint2 GetFixedPoint(this ChemMasterReagentAmount amount)
        {
            if (amount == ChemMasterReagentAmount.All)
                return FixedPoint2.MaxValue;
            else
                return FixedPoint2.New((int)amount);
        }
    }*/

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? InputContainerInfo;
        public readonly ContainerInfo? OutputContainerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents;

        public readonly ChemMasterMode Mode;

        public readonly FixedPoint2? BufferCurrentVolume;
        public readonly uint SelectedPillType;

        public readonly uint PillDosageLimit;

        public readonly bool UpdateLabel;

        public ChemPrenticeBoundUserInterfaceState(
            ChemMasterMode mode, ContainerInfo? inputContainerInfo, ContainerInfo? outputContainerInfo,
            IReadOnlyList<ReagentQuantity> bufferReagents, FixedPoint2 bufferCurrentVolume,
            uint selectedPillType, uint pillDosageLimit, bool updateLabel)
        {
            InputContainerInfo = inputContainerInfo;
            OutputContainerInfo = outputContainerInfo;
            BufferReagents = bufferReagents;
            Mode = mode;
            BufferCurrentVolume = bufferCurrentVolume;
            SelectedPillType = selectedPillType;
            PillDosageLimit = pillDosageLimit;
            UpdateLabel = updateLabel;
        }
    }

    [Serializable, NetSerializable]
    public enum ChemPrenticeUiKey
    {
        Key
    }
}

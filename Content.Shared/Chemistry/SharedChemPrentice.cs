using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedChemPrentice
    {
        public const uint PillTypes = 20;
        public const string BufferSolutionName = "buffer";
        public const string InputSlotName = "beakerSlot";
        public const string OutputSlotName = "outputSlot";
        public const string PillSolutionName = "food";
        public const string BottleSolutionName = "drink";
        public const uint LabelMaxLength = 50;
    }

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeSetModeMessage : BoundUserInterfaceMessage
    {
        public readonly ChemPrenticeMode ChemPrenticeMode;

        public ChemPrenticeSetModeMessage(ChemPrenticeMode mode)
        {
            ChemPrenticeMode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeSetPillTypeMessage : BoundUserInterfaceMessage
    {
        public readonly uint PillType;

        public ChemPrenticeSetPillTypeMessage(uint pillType)
        {
            PillType = pillType;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeReagentAmountButtonMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId ReagentId;
        public readonly ChemPrenticeReagentAmount Amount;
        public readonly bool FromBuffer;

        public ChemPrenticeReagentAmountButtonMessage(ReagentId reagentId, ChemPrenticeReagentAmount amount, bool fromBuffer)
        {
            ReagentId = reagentId;
            Amount = amount;
            FromBuffer = fromBuffer;
        }
    }

    [Serializable, NetSerializable]
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

    public enum ChemPrenticeReagentAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U25 = 25,
        U50 = 50,
        U100 = 100,
        All,
    }

    public static class ChemPrenticeReagentAmountToFixedPoint
    {
        public static FixedPoint2 GetFixedPoint(this ChemPrenticeReagentAmount amount)
        {
            if (amount == ChemPrenticeReagentAmount.All)
                return FixedPoint2.MaxValue;
            else
                return FixedPoint2.New((int)amount);
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? InputContainerInfo;
        public readonly ContainerInfo? OutputContainerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents;

        public readonly ChemPrenticeMode Mode;

        public readonly FixedPoint2? BufferCurrentVolume;
        public readonly uint SelectedPillType;

        public readonly uint PillDosageLimit;

        public readonly bool UpdateLabel;

        public ChemPrenticeBoundUserInterfaceState(
            ChemPrenticeMode mode, ContainerInfo? inputContainerInfo, ContainerInfo? outputContainerInfo,
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

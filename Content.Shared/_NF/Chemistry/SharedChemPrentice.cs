using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>

    [Serializable, NetSerializable]
    public sealed class ChemPrenticeBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? InputContainerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents;

        public readonly ChemMasterMode Mode;

        public readonly FixedPoint2? BufferCurrentVolume;

        public readonly FixedPoint2? BufferMaxVolume;

        public ChemPrenticeBoundUserInterfaceState(
            ChemMasterMode mode, ContainerInfo? inputContainerInfo,
            IReadOnlyList<ReagentQuantity> bufferReagents, FixedPoint2 bufferCurrentVolume, FixedPoint2? bufferMaxVolume)
        {
            InputContainerInfo = inputContainerInfo;
            BufferReagents = bufferReagents;
            Mode = mode;
            BufferCurrentVolume = bufferCurrentVolume;
            BufferMaxVolume = bufferMaxVolume;
        }
    }

    [Serializable, NetSerializable]
    public enum ChemPrenticeUiKey
    {
        Key
    }
}

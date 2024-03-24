using Robust.Shared.Serialization;

namespace Content.Shared.Paper
{
    [Serializable, NetSerializable]
    public sealed class PenStatus
    {
        public PenStatus(NetEntity penUid)
        {
            PenUid = penUid;
        }

        public NetEntity PenUid;
    }

    [Serializable, NetSerializable]
    public enum PenMode : byte
    {
        /// <summary>
        /// Frontier - The normal mode of a pen.
        /// </summary>
        PenWrite = 0,

        /// <summary>
        /// Frontier - The sign mode of a pen.
        /// </summary>
        PenSign = 1,
    }
}

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
        /// Sensor doesn't send any information about owner
        /// </summary>
        PenWrite = 0,

        /// <summary>
        /// Sensor sends only binary status (alive/dead)
        /// </summary>
        PenSign = 1,
    }
}

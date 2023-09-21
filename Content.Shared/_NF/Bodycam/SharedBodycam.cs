using Robust.Shared.Serialization;

namespace Content.Shared._NF.Bodycam
{
    [Serializable, NetSerializable]
    public sealed class BodycamStatus
    {
        public BodycamStatus(NetEntity bodycamUid)
        {
            BodycamUid = bodycamUid;
        }

        public NetEntity BodycamUid;
    }

    [Serializable, NetSerializable]
    public enum BodycamMode : byte
    {
        /// <summary>
        /// Sensor doesn't send any information about owner
        /// </summary>
        CameraOff = 0,

        /// <summary>
        /// Sensor sends only binary status (alive/dead)
        /// </summary>
        CameraOn = 1,
    }
}

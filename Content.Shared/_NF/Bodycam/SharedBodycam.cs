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
        /// Bodycam off
        /// </summary>
        CameraOff = 0,

        /// <summary>
        /// Bodycam on
        /// </summary>
        CameraOn = 1,
    }
}

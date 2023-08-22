using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Bodycam
{
    [Serializable, NetSerializable]
    public sealed class BodycamStatus
    {
        public BodycamStatus(EntityUid bodycamUid, string name, string job)
        {
            BodycamUid = bodycamUid;
            Name = name;
            Job = job;
        }

        public TimeSpan Timestamp;
        public EntityUid BodycamUid;
        public string Name;
        public string Job;
        public EntityCoordinates? Coordinates;
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

    public static class BodycamConstants
    {
        public const string NET_NAME = "name";
        public const string NET_JOB = "job";
        public const string NET_COORDINATES = "coords";
        public const string NET_BODYCAM_UID = "uid";
    }
}

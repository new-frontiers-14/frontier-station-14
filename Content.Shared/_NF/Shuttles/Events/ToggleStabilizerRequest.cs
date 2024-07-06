
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shuttles.Events
{
    /// <summary>
    /// Raised on the client when it wishes to not have 2 docking ports docked.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ToggleStabilizerRequest : BoundUserInterfaceMessage
    {
        public NetEntity? ShuttleEntityUid { get; set; }
        public InertiaDampeningMode Mode { get; set; }
    }

    [Serializable, NetSerializable]
    public enum InertiaDampeningMode
    {
        Off = 0,
        Dampen = 1,
        Anchored = 2,
        Station = 3,
    }
}

using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shuttles.Events
{
    /// <summary>
    /// Raised on the client when it wishes to change the IFF Siren state of a ship.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class SetIFFSirenRequest : BoundUserInterfaceMessage
    {
        public NetEntity? ShuttleEntityUid { get; set; }
        public bool SirenState { get; set; }
    }
}

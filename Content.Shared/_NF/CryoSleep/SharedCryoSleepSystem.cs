using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.CryoSleep;

public abstract partial class SharedCryoSleepSystem : EntitySystem
{
    /// <summary>
    ///   Raised on a cryopod that an entity was shoved into,
    ///   only if the mind of that entity has not decided to proceed with cryosleep or cancel it.
    ///
    ///   The target and the user of this event is the cryopod, and the "used" is the body put into it.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class CryoStoreDoAfterEvent : SimpleDoAfterEvent
    {
    }

    /// <summary>
    ///   Sent from the server to the client when the server wants to know if it has a body that is cryosleeping or not.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GetStatusMessage : EntityEventArgs
    {
        /// <summary>
        ///   Sent from the server to the client in response to a GetStatusMessage.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class Response : EntityEventArgs
        {
            public readonly bool HasCryosleepingBody;

            public Response(bool hasCryosleepingBody)
            {
                HasCryosleepingBody = hasCryosleepingBody;
            }
        }
    }

    /// <summary>
    ///   Sent from the client to the server when the client, controlling a ghost, wants to return to a cryosleeping body.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class WakeupRequestMessage : EntityEventArgs
    {
        /// <summary>
        ///   Sent from the server to the client in response to a WakeupRequestMessage.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class Response : EntityEventArgs
        {
            public readonly ReturnToBodyStatus Status;

            public Response(ReturnToBodyStatus status)
            {
                Status = status;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum ReturnToBodyStatus : byte
    {
        Success,
        Occupied,
        BodyMissing,
        NoCryopodAvailable,
        NotAGhost,
        Disabled
    }
}

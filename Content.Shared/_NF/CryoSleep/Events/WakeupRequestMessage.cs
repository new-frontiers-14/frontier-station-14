using Robust.Shared.Serialization;

namespace Content.Shared._NF.CryoSleep.Events;

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

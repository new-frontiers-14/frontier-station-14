using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.CryoSleep;

public abstract partial class SharedCryoSleepSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public sealed partial class CryoStoreDoAfterEvent : SimpleDoAfterEvent
    {
    }

    /// <summary>
    ///   Send from the client to the server when the client, controlling a ghost, wants to return to a cryosleeping body.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class WakeupRequestMessage : EntityEventArgs
    {
        /// <summary>
        ///   Send from the server to the client in response to a WakeupRequestMessage.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class Response : EntityEventArgs
        {
            public readonly ReturnToBodyResult Result;

            public Response(ReturnToBodyResult result)
            {
                Result = result;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum ReturnToBodyResult : byte
    {
        Success,
        Occupied,
        BodyMissing,
        CryopodMissing,
        NotAGhost
    }
}

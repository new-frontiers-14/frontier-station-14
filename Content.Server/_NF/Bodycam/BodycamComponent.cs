using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server._NF.Bodycam;
using Content.Server.SurveillanceCamera;

namespace Content.Shared._NF.Bodycam
{
    [RegisterComponent]
    [Access(typeof(BodycamSystem), typeof(SurveillanceCameraSystem))]
    public sealed class BodycamComponent : Component
    {
        /// <summary>
        ///     Choose a random camera mode when item is spawned.
        /// </summary>
        [DataField("randomMode")]
        public bool RandomMode = false;

        /// <summary>
        ///     If true user can't change camera mode
        /// </summary>
        [DataField("controlsLocked")]
        public bool ControlsLocked = false;

        /// <summary>
        ///     Current camera mode. Can be switched by user verbs.
        /// </summary>
        [DataField("mode")]
        public BodycamMode Mode = BodycamMode.CameraOff;

        /// <summary>
        ///     Activate camera if user wear it in this slot.
        /// </summary>
        [DataField("activationSlot")]
        public string ActivationSlot = "neck";

        /// <summary>
        /// Activate camera if user has this in a camera-compatible container.
        /// </summary>
        [DataField("activationContainer")]
        public string? ActivationContainer;

        /// <summary>
        ///     How often does camera update its owners status (in seconds). Limited by the system update rate.
        /// </summary>
        [DataField("updateRate")]
        public TimeSpan UpdateRate = TimeSpan.FromSeconds(2f);

        /// <summary>
        ///     Current user that wears camera. Null if nobody wearing it.
        /// </summary>
        [ViewVariables]
        public EntityUid? User = null;

        /// <summary>
        ///     Next time when camera updated owners status
        /// </summary>
        [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate = TimeSpan.Zero;
    }
}

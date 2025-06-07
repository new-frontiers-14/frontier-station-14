using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.DeviceLinking.Components
{
    /// <summary>
    /// Key representing which <see cref="PlayerBoundUserInterface"/> is currently open.
    /// </summary>
    [Serializable, NetSerializable]
    public enum RngDeviceUiKey
    {
        Key,
    }

    #region Enums

    /// <summary>
    /// Last state of a signal port, used to not spam invoking ports.
    /// </summary>
    [Serializable, NetSerializable]
    public enum SignalState : byte
    {
        Momentary = 0, // Instantaneous pulse high, compatibility behavior
        Low = 1,
        High = 2
    }

    #endregion

    /// <summary>
    /// Shared component for RNG device that contains UI-relevant data that needs to be networked to clients.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class RngDeviceComponent : Component
    {
        /// <summary>
        /// Number of output ports this device has.
        /// </summary>
        [DataField, AutoNetworkedField]
        public int Outputs = 6;

        /// <summary>
        /// Current signal state of the device
        /// </summary>
        [DataField("state"), AutoNetworkedField]
        public SignalState State { get; set; } = SignalState.Low;

        /// <summary>
        /// Whether the device is muted
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Muted;

        /// <summary>
        /// Target number for percentile dice (1-100). Only used when Outputs = 2.
        /// </summary>
        [DataField, AutoNetworkedField]
        public int TargetNumber = 50;

        /// <summary>
        /// When enabled, sends High signal to selected port and Low signals to others
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool EdgeMode;

        /// <summary>
        /// The last value rolled (1-100 for percentile, 1-N for other dice)
        /// </summary>
        [DataField, AutoNetworkedField]
        public int LastRoll;

        /// <summary>
        /// The last output port that was triggered
        /// </summary>
        [DataField, AutoNetworkedField]
        public int LastOutputPort;
    }

    [Serializable, NetSerializable]
    public sealed class RngDeviceToggleMuteMessage : BoundUserInterfaceMessage
    {
        public bool Muted { get; }

        public RngDeviceToggleMuteMessage(bool muted)
        {
            Muted = muted;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RngDeviceToggleEdgeModeMessage : BoundUserInterfaceMessage
    {
        public bool EdgeMode { get; }

        public RngDeviceToggleEdgeModeMessage(bool edgeMode)
        {
            EdgeMode = edgeMode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RngDeviceSetTargetNumberMessage : BoundUserInterfaceMessage
    {
        public int TargetNumber { get; }

        public RngDeviceSetTargetNumberMessage(int targetNumber)
        {
            TargetNumber = targetNumber;
        }
    }
}

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
        Momentary, // Instantaneous pulse high, compatibility behavior
        Low,
        High
    }

    #endregion

    /// <summary>
    /// Shared component for RNG device that contains networked UI state data
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class RngDeviceComponent : Component
    {
        /// <summary>
        /// Number of output ports this device has.
        /// </summary>
        [DataField("outputs"), AutoNetworkedField]
        public int Outputs { get; set; } = 6;

        /// <summary>
        /// Current signal state of the device
        /// </summary>
        [DataField("state"), AutoNetworkedField]
        public SignalState State { get; set; } = SignalState.Low;

        /// <summary>
        /// Whether the device is muted
        /// </summary>
        [DataField("muted"), AutoNetworkedField]
        public bool Muted { get; set; }

        /// <summary>
        /// Target number for percentile dice (1-100). Only used when Outputs = 2.
        /// </summary>
        [DataField("targetNumber"), AutoNetworkedField]
        public int TargetNumber { get; set; } = 50;

        /// <summary>
        /// When enabled, sends High signal to selected port and Low signals to others
        /// </summary>
        [DataField("edgeMode"), AutoNetworkedField]
        public bool EdgeMode { get; set; }

        /// <summary>
        /// The last value rolled (1-100 for percentile, 1-N for other dice)
        /// </summary>
        [DataField("lastRoll"), AutoNetworkedField]
        public int LastRoll { get; set; }

        /// <summary>
        /// The last output port that was triggered
        /// </summary>
        [DataField("lastOutputPort"), AutoNetworkedField]
        public int LastOutputPort { get; set; }
    }

    /// <summary>
    /// Represents the state of an RNG device that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RngDeviceBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int LastRoll { get; }
        public int LastOutputPort { get; }
        public bool Muted { get; }
        public bool EdgeMode { get; }
        public int TargetNumber { get; }
        public int Outputs { get; }
        public SignalState State { get; }

        public RngDeviceBoundUserInterfaceState(
            int lastRoll,
            int lastOutputPort,
            bool muted,
            bool edgeMode,
            int targetNumber,
            int outputs,
            SignalState state)
        {
            LastRoll = lastRoll;
            LastOutputPort = lastOutputPort;
            Muted = muted;
            EdgeMode = edgeMode;
            TargetNumber = targetNumber;
            Outputs = outputs;
            State = state;
        }
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

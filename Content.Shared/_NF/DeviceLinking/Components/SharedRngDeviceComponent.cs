using Robust.Shared.Serialization;

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

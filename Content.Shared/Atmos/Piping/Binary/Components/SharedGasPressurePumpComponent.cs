using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components
{
    [Serializable, NetSerializable]
    public enum GasPressurePumpUiKey
    {
        Key,
        BidiKey, // Frontier: bidirectional pumps
    }

    [Serializable, NetSerializable]
    public sealed class GasPressurePumpBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string PumpLabel { get; }
        public float OutputPressure { get; }
        public bool Enabled { get; }
        public bool Inward { get; } // Frontier

        public GasPressurePumpBoundUserInterfaceState(string pumpLabel, float outputPressure, bool enabled, bool inward = false) // Frontier: added inward
        {
            PumpLabel = pumpLabel;
            OutputPressure = outputPressure;
            Enabled = enabled;
            Inward = inward; // Frontier
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasPressurePumpToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasPressurePumpToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasPressurePumpChangeOutputPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; }

        public GasPressurePumpChangeOutputPressureMessage(float pressure)
        {
            Pressure = pressure;
        }
    }
}

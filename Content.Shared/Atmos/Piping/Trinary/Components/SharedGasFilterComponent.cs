using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components
{
    [Serializable, NetSerializable]
    public enum GasFilterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class GasFilterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string FilterLabel { get; }
        public float TransferRate { get; }
        public bool Enabled { get; }
        public HashSet<Gas>? FilterGases { get; } // Funky Station

        public GasFilterBoundUserInterfaceState(string filterLabel, float transferRate, bool enabled, HashSet<Gas>? filterGases) // Funky Station
        {
            FilterLabel = filterLabel;
            TransferRate = transferRate;
            Enabled = enabled;
            FilterGases = filterGases; // Funky Station
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasFilterToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasFilterToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasFilterChangeRateMessage : BoundUserInterfaceMessage
    {
        public float Rate { get; }

        public GasFilterChangeRateMessage(float rate)
        {
            Rate = rate;
        }
    }
    // Funky Station Start - Changed to hashset, function and variable names changed
    [Serializable, NetSerializable]
    public sealed class GasFilterChangeGasesMessage : BoundUserInterfaceMessage
    {
        public HashSet<Gas> Gases { get; }

        public GasFilterChangeGasesMessage(HashSet<Gas> gases)
        {
            Gases = gases;
        }
    }
    // Funky Station End - Changed to hashset, function and variable names changed
}

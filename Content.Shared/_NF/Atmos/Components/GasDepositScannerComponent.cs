using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasDepositScannerComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public EntityUid User;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [Serializable, NetSerializable]
    public enum GasDepositScannerUiKey
    {
        Key,
    }

    /// <summary>
    /// Atmospheric data is gathered in the system and sent to the user
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GasDepositScannerUserMessage : BoundUserInterfaceMessage
    {
        public GasEntry[] Gases;
        public NetEntity DepositUid;
        public string? Error;
        public GasDepositScannerUserMessage(GasEntry[] gases, NetEntity depositUid, string? error = null)
        {
            Gases = gases;
            DepositUid = depositUid;
            Error = error;
        }
    }

    public enum ApproximateGasDepositSize
    {
        Trace,
        Small,
        Medium,
        Large,
        Enormous
    }

    /// <summary>
    /// Individual gas entry data for populating the UI
    /// </summary>
    [Serializable, NetSerializable]
    public struct GasEntry(string name, ApproximateGasDepositSize amount)
    {
        public readonly string Name = name;
        public readonly ApproximateGasDepositSize Amount = amount;
    }

    [Serializable, NetSerializable]
    public sealed class GasDepositScannerDisableMessage : BoundUserInterfaceMessage
    {

    }
}

[Serializable, NetSerializable]
public enum GasDepositScannerVisuals : byte
{
    Enabled,
}


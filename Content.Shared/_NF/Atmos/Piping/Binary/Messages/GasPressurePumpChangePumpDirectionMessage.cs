
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Piping.Binary.Messages;

[Serializable, NetSerializable]
public sealed class GasPressurePumpChangePumpDirectionMessage(bool inwards) : BoundUserInterfaceMessage
{
    public bool Inwards { get; } = inwards;
}

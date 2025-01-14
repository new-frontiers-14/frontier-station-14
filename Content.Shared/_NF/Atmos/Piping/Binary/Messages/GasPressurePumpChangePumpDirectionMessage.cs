
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Piping.Binary.Messages;

[Serializable, NetSerializable]
public sealed class GasPressurePumpChangePumpDirectionMessage : BoundUserInterfaceMessage
{
    public bool Inwards { get; }

    public GasPressurePumpChangePumpDirectionMessage(bool inwards)
    {
        Inwards = inwards;
    }
}

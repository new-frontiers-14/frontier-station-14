
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Piping.Binary.Components;

[Serializable, NetSerializable]
public sealed class GasPressurePumpChangePumpDirectionMessage : BoundUserInterfaceMessage
{
    public bool Inwards { get; }

    public GasPressurePumpChangePumpDirectionMessage(bool inwards)
    {
        Inwards = inwards;
    }
}

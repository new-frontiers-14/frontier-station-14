using Robust.Shared.GameObjects;

namespace Content.Shared._NF.DeviceLinking.Events;

/// <summary>
/// Event raised when the RNG device should roll
/// </summary>
public sealed class RollEvent : HandledEntityEventArgs
{
    public int Outputs { get; }
    public EntityUid? User { get; }

    public RollEvent(int outputs, EntityUid? user = null)
    {
        Outputs = outputs;
        User = user;
    }
}

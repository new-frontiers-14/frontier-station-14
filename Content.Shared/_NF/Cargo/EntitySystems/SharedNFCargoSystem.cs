using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Cargo;

public abstract partial class SharedNFCargoSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}

[NetSerializable, Serializable]
public enum NFCargoConsoleUiKey : byte
{
    Orders
}


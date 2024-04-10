using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market;

public abstract class SharedMarketSystem : EntitySystem {}

[NetSerializable, Serializable]
public enum MarketConsoleUiKey : byte
{
    Default
}

using Robust.Shared.Prototypes;

namespace Content.Shared._UNEON.VipUplink;

/// <summary>Server-authoritative catalogue. Personal products require an account whitelist AND a tier.</summary>
[Prototype]
public sealed partial class VipUplinkProductPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField(required: true)] public LocId Name;
    [DataField(required: true)] public EntProtoId Product;
    [DataField(required: true)] public int Price;
    [DataField] public int MinimumTier = 1;
    /// <summary>SS14 account UUIDs, never character names or Discord IDs. Empty means a shared product.</summary>
    [DataField] public HashSet<string> AllowedAccounts = new();

    public bool IsAvailable(int tier, string account)
    {
        return Price > 0 && MinimumTier > 0 && tier >= MinimumTier &&
               (AllowedAccounts.Count == 0 || AllowedAccounts.Contains(account));
    }
}

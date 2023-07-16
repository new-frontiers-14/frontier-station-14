using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.StationBounties;

namespace Content.Server._NF.BountyContracts;

public sealed class BountyContractsSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BountyContractsComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, BountyContractsComponent component, CartridgeUiReadyEvent args)
    {
        var targets = new List<BountyContractTargetInfo>
        {
            new() { Name = "Bill", DNA = "ACGTGCA" },
            new() { Name = "Joe Doe", DNA = "GTTCCAA" },
            new() { Name = "Alan Poe", DNA = "ACGGGTAACC" },
            new() { Name = "George Clinton", DNA = "CCCGGGTTAA" },
            new() { Name = "Cuban Pete", DNA = "AAACGGGTTTAA" }
        };

        var vessels = new List<string>()
        {
            "NT-16 324",
            "NT-32 123",
            "NT-324 3212",
            "NT-23 324",
            "NT-16 12"
        };

        var state = new BountyContractCreateBoundUserInterfaceState(targets, vessels);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(args.Loader, state);
    }
}

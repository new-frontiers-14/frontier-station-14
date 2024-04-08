namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
    }
}

using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market;

[Virtual, NetSerializable, Serializable]
public class MarketData
{
    public string Prototype { get; set; }
    public int Quantity { get; set; }
    public NetEntity StationUid { get; set; }

    public MarketData(string prototype, int quantity, NetEntity stationUid)
    {
        Prototype = prototype;
        Quantity = quantity;
        StationUid = stationUid;
    }
}

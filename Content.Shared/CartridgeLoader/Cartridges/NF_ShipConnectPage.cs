using Robust.Shared.Prototypes;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Prototype("NF_shipConnectPage")]
public sealed partial class NF_ShipConnectPage : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = "";

    [DataField("onStart")]
    public string? OnStart { get; private set; }

    [DataField("locKey")]
    public string? LocKey { get; private set; }

    [DataField("onYes")]
    public string? OnYes { get; private set; }

    [DataField("onNo")]
    public string? OnNo { get; private set; }

    [DataField("locKeyTitle")]
    public string? LocKeyTitle { get; private set; }

    [DataField("locKeyDescription")]
    public string? LocKeyDescription { get; private set; }

    [DataField("locKeySeverity")]
    public string? LocKeySeverity { get; private set; }

    [DataField("locKeyPunishment")]
    public string? LocKeyPunishment { get; private set; }
}

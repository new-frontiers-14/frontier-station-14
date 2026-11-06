using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MedTekCartridgePrinterComponent : Component
{
    /// <summary>
    /// Prototype of an entity to use as a template for printing. The paper may contain placeholders (wrapped in braces)
    /// which will be filled in during printing.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<PaperComponent> PrintTemplate;
}

using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Medical;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HealthAnalyzerPrinterComponent : Component
{
    /// <summary>
    /// Time when the component can print again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan PrintAllowedAfter = TimeSpan.Zero;

    /// <summary>
    /// Cooldown between individual prints
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Prototype of an entity to use as a template for printing. The paper may contain placeholders (wrapped in braces)
    /// which will be filled in during printing.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> PrintTemplate;
}

using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     changes the gases that a plant converts between.
/// </summary>
public sealed partial class PlantMutateConvertGases : EventEntityEffect<PlantMutateConvertGases>
{
    [DataField]
    public float MinConvertAmount = 0.01f;

    [DataField]
    public float MaxConvertAmount = 0.5f;
    [DataField]
    public float MinScaleFactor = 1.0f;
    [DataField]
    public float MaxScaleFactor = 1.4f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

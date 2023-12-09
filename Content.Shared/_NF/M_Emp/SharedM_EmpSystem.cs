using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.M_Emp;

public abstract class SharedM_EmpSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
}

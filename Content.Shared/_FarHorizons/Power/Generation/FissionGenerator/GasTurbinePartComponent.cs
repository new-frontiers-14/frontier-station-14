using Content.Shared._FarHorizons.Materials;
using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

public abstract partial class GasTurbinePartComponent : Component
{
    [Dependency] private IPrototypeManager _proto = default!;

    [DataField("material")]
    public ProtoId<MaterialPrototype> Material = "Steel";

    public MaterialProperties Properties
    {
        get
        {
            IoCManager.Resolve(ref _proto);
            _properties ??= new MaterialProperties(_proto.Index(Material).Properties);

            return _properties;
        }
        set => _properties = value;
    }
    [DataField("properties")]
    private MaterialProperties? _properties;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class GasTurbineBladeComponent : GasTurbinePartComponent;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasTurbineStatorComponent : GasTurbinePartComponent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Medical.Limbs;

public interface IImplantable
{
}

public partial interface IWithAction : IImplantable
{
    public bool EntityIcon { get; }

    public EntProtoId Action { get; }

    public EntityUid? ActionEntity { get; set; }
}

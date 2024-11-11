using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Construction.Components
{
    /// <summary>
    /// Used for construction graphs in building wallmount computers.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ComputerWallmountBoardComponent : Component
    {
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }
    }
}

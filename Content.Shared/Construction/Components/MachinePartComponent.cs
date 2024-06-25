// FRONTIER MERGE: restored this from frontier's master to get things to compile

using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MachinePartComponent : Component
    {
        [DataField("part", required: true)]
        public ProtoId<MachinePartPrototype> PartType { get; private set; } = default!; // Frontier: used ProtoId explicitly

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rating")]
        public int Rating { get; private set; } = 1;

        /// <summary>
        ///     This number is used in tests to ensure that you can't use high quality machines for arbitrage. In
        ///     principle there is nothing wrong with using higher quality parts, but you have to be careful to not
        ///     allow them to be put into a lathe or something like that.
        /// </summary>
        public const int MaxRating = 4;
    }
}

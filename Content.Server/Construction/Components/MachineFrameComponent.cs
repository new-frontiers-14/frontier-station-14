using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes; // Frontier: upgradeable machine parts
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary; // Frontier: upgradeable machine parts

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed partial class MachineFrameComponent : Component
    {
        public const string PartContainerName = "machine_parts";
        public const string BoardContainerName = "machine_board";

        [ViewVariables]
        public bool HasBoard => BoardContainer?.ContainedEntities.Count != 0;

        [ViewVariables] // Frontier: upgradeable machine parts
        public Dictionary<ProtoId<MachinePartPrototype>, int> Progress = new(); // Frontier: upgradeable machine parts

        [ViewVariables]
        public readonly Dictionary<ProtoId<StackPrototype>, int> MaterialProgress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> ComponentProgress = new();

        [ViewVariables]
        public readonly Dictionary<ProtoId<TagPrototype>, int> TagProgress = new();

        [ViewVariables] // Frontier: upgradeable machine parts
        public Dictionary<ProtoId<MachinePartPrototype>, int> Requirements = new(); // Frontier: upgradeable machine parts

        [ViewVariables]
        public Dictionary<ProtoId<StackPrototype>, int> MaterialRequirements = new();

        [ViewVariables]
        public Dictionary<string, GenericPartInfo> ComponentRequirements = new();

        [ViewVariables]
        public Dictionary<ProtoId<TagPrototype>, GenericPartInfo> TagRequirements = new();

        [ViewVariables]
        public Container BoardContainer = default!;

        [ViewVariables]
        public Container PartContainer = default!;
    }
}

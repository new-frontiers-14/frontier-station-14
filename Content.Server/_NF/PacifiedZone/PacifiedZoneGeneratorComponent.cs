using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Roles;

namespace Content.Server._NF.PacifiedZone
{
    [RegisterComponent]
    public sealed partial class PacifiedZoneGeneratorComponent : Component
    {
        [ViewVariables]
        public List<EntityUid> TrackedEntities = new();

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;

        /// <summary>
        /// The interval at which this component updates.
        /// </summary>
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

        [DataField]
        public int Radius = 5;

        [DataField]
        public List<ProtoId<JobPrototype>> ImmuneRoles = new();
    }
}
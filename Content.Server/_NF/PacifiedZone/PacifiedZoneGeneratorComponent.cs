using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.Server._NF.PacifiedZone
{
    [RegisterComponent]
    public sealed partial class PacifiedZoneGeneratorComponent : Component
    {
        public List<NetEntity> OldListEntities = new();
        public List<NetEntity> IntermediateListEntities = new();

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;

        /// <summary>
        /// The interval at which this component updates.
        /// </summary>
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

        [DataField("radius")]
        public int Radius = 5;

        [DataField("rolesImmun")]
        public List<ProtoId<JobPrototype>> RolesImmun = new();
    }
}
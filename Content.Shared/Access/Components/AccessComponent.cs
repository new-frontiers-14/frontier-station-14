using Content.Shared.Access.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access.Components
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(SharedAccessSystem))]
    [AutoGenerateComponentState]
    public sealed partial class AccessComponent : Component
    {
        /// <summary>
        /// True if the access provider is enabled and can grant access.
        /// </summary>
        [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool Enabled = true;

        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
        [Access(typeof(SharedAccessSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        [AutoNetworkedField]
        public HashSet<string> Tags = new();

        /// <summary>
        ///     Access Groups. These are added to the tags during map init. After map init this will have no effect.
        /// </summary>
        [DataField("groups", readOnly: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessGroupPrototype>))]
        [AutoNetworkedField]
        public HashSet<string> Groups = new();
    }

    /// <summary>
    /// Event raised on an entity to find additional entities which provide access.
    /// </summary>
    [ByRefEvent]
    public struct GetAdditionalAccessEvent
    {
        public HashSet<EntityUid> Entities = new();

        public GetAdditionalAccessEvent()
        {
        }
    }

    [ByRefEvent]
    public record struct GetAccessTagsEvent(HashSet<string> Tags, IPrototypeManager PrototypeManager)
    {
        public void AddGroup(string group)
        {
            if (!PrototypeManager.TryIndex<AccessGroupPrototype>(group, out var groupPrototype))
                return;

            Tags.UnionWith(groupPrototype.Tags);
        }
    }
}

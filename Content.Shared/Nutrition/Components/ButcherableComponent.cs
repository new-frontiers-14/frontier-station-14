using Content.Shared.Kitchen;
using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization; // EE

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Indicates that the entity can be butchered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ButcherableComponent : Component
{
    /// <summary>
    /// List of the entities that this entity should spawn after being butchered.
    /// </summary>
    /// <remarks>
    /// Note that <see cref="SharedKitchenSpikeSystem"/> spawns one item at a time and decreases the amount until it's zero and then removes the entry.
    /// </remarks>
    [DataField("spawned", required: true), AutoNetworkedField]
    public List<EntitySpawnEntry> SpawnedEntities = [];

    /// <summary>
    /// Time required to butcher that entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ButcherDelay = 8.0f;

<<<<<<< HEAD
        [ViewVariables(VVAccess.ReadWrite), DataField("butcheringType")]
        public ButcheringType Type = ButcheringType.Knife;

        /// <summary>
        /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
        /// </summary>
        [ViewVariables]
        public bool BeingButchered;
    }

    [Serializable, NetSerializable] // EE
    public enum ButcheringType : byte
    {
        Knife, // e.g. goliaths
        Spike, // e.g. monkeys
        Gibber // e.g. humans. TODO
    }
=======
    /// <summary>
    /// Tool type used to butcher that entity.
    /// </summary>
    [DataField("butcheringType"), AutoNetworkedField]
    public ButcheringType Type = ButcheringType.Knife;
}

public enum ButcheringType : byte
{
    /// <summary>
    /// E.g. goliaths.
    /// </summary>
    Knife,

    /// <summary>
    /// E.g. monkeys.
    /// </summary>
    Spike,

    /// <summary>
    /// E.g. humans.
    /// </summary>
    Gibber // TODO
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
}

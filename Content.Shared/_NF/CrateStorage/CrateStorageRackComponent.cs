using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.CrateStorage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrateStorageRackComponent : Component
{
    /// <summary>
    /// Sounds played when a crate is being moved in or out of the rack.
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? MoveCrateSound = new SoundPathSpecifier("/Audio/Machines/disposalflush.ogg");

    /// <summary>
    /// The amount of crates that can be stored in this rack, used for visual state.
    /// </summary>
    [DataField]
    public int Capacity = 2;

    /// <summary>
    /// The amount of crates stored in this rack, used for visual state.
    /// </summary>
    [ViewVariables]
    public int StoredCrates;

    /// <summary>
    /// Whether the machine is powered or not
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Powered;
};

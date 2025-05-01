using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class NoShoesNoFrictionComponent : Component
{
    /// <summary>
    /// Slot the clothing has to not be worn in to work.
    /// </summary>
    [DataField]
    public string Slot = "shoes";

    /// <summary>
    /// Modified mob friction while having no shoes
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float MobFriction = 0.5f;

    /// <summary>
    /// Modified mob friction while having no shoes
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float MobFrictionNoInput = 0.05f;

    /// <summary>
    /// Modified mob acceleration while having no shoes
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float MobAcceleration = 2.0f;

    /// <summary>
    /// Blacklist shoes to ignore, eg. Skates
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist { get; private set; } = new();
}

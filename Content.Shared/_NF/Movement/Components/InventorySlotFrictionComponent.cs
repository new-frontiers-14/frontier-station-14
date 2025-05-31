using Robust.Shared.GameStates;

namespace Content.Shared._NF.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class InventorySlotFrictionComponent : Component
{
    /// <summary>
    /// The inventory slot that controls
    /// </summary>
    [DataField]
    public string Slot = "shoes";

    /// <summary>
    /// If true, the slot has to be full to apply this friction
    /// </summary>
    [DataField]
    public bool Full;

    /// <summary>
    /// Modified friction while the slot is empty.
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float Friction = 0.5f;

    /// <summary>
    /// Modified friction while having no shoes
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float FrictionNoInput = 0.05f;

    /// <summary>
    /// Modified acceleration while having no shoes
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float Acceleration = 2.0f;
}

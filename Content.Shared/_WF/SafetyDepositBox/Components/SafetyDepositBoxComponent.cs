using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.SafetyDepositBox.Components;

/// <summary>
/// A physical box that stores items and can be deposited into a console for persistence.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SafetyDepositBoxComponent : Component
{
    /// <summary>
    /// Unique ID for this deposit box, assigned when purchased.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Guid? BoxId;

    /// <summary>
    /// The user ID of the owner of this box.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Guid? OwnerId;

    /// <summary>
    /// The character profile index (slot number) of the owner.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int? CharacterIndex;

    /// <summary>
    /// Display name of the owner.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? OwnerName;

    /// <summary>
    /// The prototype ID of this box (SafetyDepositBoxSmall, Medium, or Large).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? BoxPrototypeId;
}

[Serializable, NetSerializable]
public enum SafetyDepositBoxVisuals : byte
{
    Locked,
}

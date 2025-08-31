using Robust.Shared.Serialization;

namespace Content.Shared._NF.Manufacturing;

[Serializable, NetSerializable]
public enum EntitySpawnMaterialVisuals : byte
{
    /// <summary>
    /// Whether or not the machine has enough materials to continue processing a unit.
    /// </summary>
    SufficientMaterial
}

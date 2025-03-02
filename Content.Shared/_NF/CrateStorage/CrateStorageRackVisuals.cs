using Robust.Shared.Serialization;

namespace Content.Shared._NF.CrateStorage;

[Serializable, NetSerializable]
public enum CrateStorageRackVisualState : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum CrateStorageRackVisuals : byte
{
    VisualState,
}

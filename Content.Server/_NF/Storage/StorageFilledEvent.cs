namespace Content.Server.Storage.Events;

/// <summary>
/// Raised when a <see cref="StorageFillComponent"/> is finished filling the storage
/// </summary>
[ByRefEvent]
public record struct StorageFilledEvent;

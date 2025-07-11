namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Interface for magnet pickup components to enable generic handling
/// </summary>
public interface IBaseMagnetPickupComponent
{
    TimeSpan NextScan { get; set; }
    float Range { get; set; }
    bool MagnetEnabled { get; set; }
    int MagnetTogglePriority { get; set; }

    // Auto-disable functionality
    bool AutoDisableEnabled { get; set; }
    TimeSpan AutoDisableTime { get; set; }
    TimeSpan LastSuccessfulPickup { get; set; }
    
    // Never disable option - when true, magnet runs indefinitely without auto-disable or manual toggle capability
    bool AlwaysOn { get; set; }
}

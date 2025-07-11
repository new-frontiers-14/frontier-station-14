namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Interface for magnet pickup components to enable generic handling
/// </summary>
public interface IBaseMagnetPickupComponent
{
    TimeSpan NextScan { get; set; }
    float Range { get; set; }
    bool MagnetEnabled { get; set; }
    bool MagnetCanBeEnabled { get; set; }
    int MagnetTogglePriority { get; set; }
}

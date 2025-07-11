namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Interface for magnet pickup components to enable generic handling
/// </summary>
public interface IBaseMagnetPickupComponent
{
    TimeSpan NextScan { get; set; }
    float Range { get; }
    bool MagnetEnabled { get; set; }
}

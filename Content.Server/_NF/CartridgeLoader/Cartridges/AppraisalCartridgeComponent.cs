using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class AppraisalCartridgeComponent : Component
{
    /// <summary>
    /// The list of appraised items
    /// </summary>
    [DataField]
    public List<AppraisedItem> AppraisedItems = new();

    /// <summary>
    /// Limits the amount of items that can be saved
    /// </summary>
    [DataField]
    public int MaxSavedItems { get; set; } = 9;

    [DataField]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}

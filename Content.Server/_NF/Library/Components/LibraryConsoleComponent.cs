using Content.Server._NF.Library.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Server._NF.Library.Components;

/// <summary>
/// Component for the library console. Allows uploading and downloading books.
/// </summary>
[RegisterComponent]
[Access(typeof(LibraryConsoleSystem))]
public sealed partial class LibraryConsoleComponent : Component
{
    /// <summary>
    /// The slot where a player inserts a BookRandomSimple to read its content for uploading.
    /// </summary>
    [DataField]
    public ItemSlot BookSlot = new();

    /// <summary>
    /// Sound played on a successful upload.
    /// </summary>
    [DataField]
    public SoundSpecifier UploadSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Sound played on a successful download (printing to the inserted book).
    /// </summary>
    [DataField]
    public SoundSpecifier DownloadSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound played when the action cannot be completed.
    /// </summary>
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
}

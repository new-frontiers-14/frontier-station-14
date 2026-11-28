using System.Collections.Generic;
using Content.Shared._NF.Library.Events;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Library.BUI;

[NetSerializable, Serializable]
public sealed class LibraryConsoleBoundUserInterfaceState(
    bool enabled,
    string? bookName = null,
    string? bookContent = null,
    List<LibraryBookData>? books = null) : BoundUserInterfaceState
{
    /// <summary>
    /// Whether or not the buttons on the interface are enabled.
    /// </summary>
    public bool Enabled = enabled;

    /// <summary>
    /// The name of the inserted book entity, or null if no book is inserted.
    /// </summary>
    public string? BookName = bookName;

    /// <summary>
    /// The content of the inserted book entity, or null if no book is inserted.
    /// </summary>
    public string? BookContent = bookContent;

    /// <summary>
    /// All library books available on this server, for the browse tab.
    /// </summary>
    public List<LibraryBookData>? Books = books;
}

[Serializable, NetSerializable]
public enum LibraryConsoleUiKey : byte
{
    Key,
}

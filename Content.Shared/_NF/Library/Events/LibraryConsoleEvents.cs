using Robust.Shared.Serialization;

namespace Content.Shared._NF.Library.Events;

/// <summary>
/// Raised by a client requesting to upload a book to the server's library database.
/// </summary>
[Serializable, NetSerializable]
public sealed class LibraryConsoleUploadBookMessage : BoundUserInterfaceMessage
{
    public string Title;
    public string Author;
    public string Content;

    public LibraryConsoleUploadBookMessage(string title, string author, string content)
    {
        Title = title;
        Author = author;
        Content = content;
    }
}

/// <summary>
/// Raised by a client requesting to download a library book onto the inserted book entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class LibraryConsoleDownloadBookMessage : BoundUserInterfaceMessage
{
    public int BookId;

    public LibraryConsoleDownloadBookMessage(int bookId)
    {
        BookId = bookId;
    }
}

/// <summary>
/// Serializable summary of a library book, sent to the client via BUI state.
/// Does not include the full content to keep state transfers lightweight.
/// </summary>
[Serializable, NetSerializable]
public sealed class LibraryBookData
{
    public int Id;
    public string Title;
    public string Author;
    public string Date;

    public LibraryBookData(int id, string title, string author, string date)
    {
        Id = id;
        Title = title;
        Author = author;
        Date = date;
    }
}

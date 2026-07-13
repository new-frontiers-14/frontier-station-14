using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Library;

/// <summary>
/// EUI state sent from server to client containing all library books with full details.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminLibraryEuiState : EuiStateBase
{
    public List<AdminLibraryBookEntry> Books { get; }

    public AdminLibraryEuiState(List<AdminLibraryBookEntry> books)
    {
        Books = books;
    }
}

/// <summary>
/// Full details for a single library book, including content and uploader player user ID.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminLibraryBookEntry
{
    public int Id;
    public string Title;
    public string Author;
    public string Content;
    public string Date;
    public Guid AuthorPlayerUserId;

    public AdminLibraryBookEntry(int id, string title, string author, string content, string date, Guid authorPlayerUserId)
    {
        Id = id;
        Title = title;
        Author = author;
        Content = content;
        Date = date;
        AuthorPlayerUserId = authorPlayerUserId;
    }
}

/// <summary>
/// Messages sent from the admin library client UI to the server EUI.
/// </summary>
public static class AdminLibraryEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase { }

    [Serializable, NetSerializable]
    public sealed class DeleteBook : EuiMessageBase
    {
        public int BookId { get; }

        public DeleteBook(int bookId)
        {
            BookId = bookId;
        }
    }
}

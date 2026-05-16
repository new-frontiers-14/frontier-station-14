using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._NF.Library;
using Content.Shared.Eui;

namespace Content.Server._NF.Library;

/// <summary>
/// Server-side EUI for the admin library panel.
/// Loads all library books from the database and handles deletion requests.
/// </summary>
public sealed class AdminLibraryEui : BaseEui
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;

    private List<AdminLibraryBookEntry> _books = new();

    public AdminLibraryEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        _ = LoadBooksAsync();
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminLibraryEuiState(_books);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AdminLibraryEuiMsg.Close:
                Close();
                break;

            case AdminLibraryEuiMsg.DeleteBook deleteMsg:
                _ = DeleteBookAsync(deleteMsg.BookId);
                break;
        }
    }

    private async Task LoadBooksAsync()
    {
        var server = await _serverDbEntry.ServerEntity;
        var books = await _dbManager.GetLibraryBooksAsync(server.Id);

        _books = books
            .Select(b => new AdminLibraryBookEntry(b.Id, b.Title, b.Author, b.Content, b.Date, b.AuthorCKey))
            .ToList();

        StateDirty();
    }

    private async Task DeleteBookAsync(int bookId)
    {
        await _dbManager.DeleteLibraryBookAsync(bookId);
        await LoadBooksAsync();
    }
}

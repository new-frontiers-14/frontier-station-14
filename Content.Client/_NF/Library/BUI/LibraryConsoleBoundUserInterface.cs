using Content.Client._NF.Library.UI;
using Content.Shared._NF.Library.BUI;
using Content.Shared._NF.Library.Events;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Library.BUI;

public sealed class LibraryConsoleBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private LibraryConsoleMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<LibraryConsoleMenu>();
        _menu.UploadBookRequested += OnUploadBook;
        _menu.DownloadBookRequested += OnDownloadBook;
        _menu.TargetBookButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("bookConsole_Book"));
    }

    private void OnUploadBook(string title, string author, string content)
    {
        SendMessage(new LibraryConsoleUploadBookMessage(title, author, content));
    }

    private void OnDownloadBook(int bookId)
    {
        SendMessage(new LibraryConsoleDownloadBookMessage(bookId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LibraryConsoleBoundUserInterfaceState libraryState)
            return;

        if (_menu is { } menu)
        {
            menu.SetEnabled(libraryState.Enabled);
            menu.SetBookContent(libraryState.Enabled, libraryState.BookName, libraryState.BookContent);
            menu.PopulateBookList(libraryState.Books);
        }
    }
}

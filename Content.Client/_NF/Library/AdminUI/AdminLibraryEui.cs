using Content.Client._NF.Library.AdminUI;
using Content.Client.Eui;
using Content.Shared._NF.Library;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._NF.Library.AdminUI;

/// <summary>
/// Client-side EUI for the admin library panel.
/// </summary>
[UsedImplicitly]
public sealed class AdminLibraryEui : BaseEui
{
    private readonly AdminLibraryWindow _window;

    public AdminLibraryEui()
    {
        _window = new AdminLibraryWindow();
        _window.OnClose += () => SendMessage(new AdminLibraryEuiMsg.Close());
        _window.OnDeleteBook += bookId => SendMessage(new AdminLibraryEuiMsg.DeleteBook(bookId));
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminLibraryEuiState cast)
            return;

        _window.PopulateBooks(cast.Books);
    }
}

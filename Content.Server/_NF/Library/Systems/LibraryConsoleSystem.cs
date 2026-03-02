using System.Linq;
using System.Threading.Tasks;
using Content.Server._NF.Library.Components;
using Content.Server.Database;
using Content.Shared._NF.Library.BUI;
using Content.Shared._NF.Library.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._NF.Library.Systems;

/// <summary>
/// Handles the library console. Opens the UI, allows the user to upload and download books.
/// </summary>
public sealed class LibraryConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private const string BookSlotId = "bookConsole_Book";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LibraryConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<LibraryConsoleComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<LibraryConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<LibraryConsoleComponent, LibraryConsoleUploadBookMessage>(OnUploadBook);
        SubscribeLocalEvent<LibraryConsoleComponent, LibraryConsoleDownloadBookMessage>(OnDownloadBook);
        SubscribeLocalEvent<LibraryConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<LibraryConsoleComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<LibraryConsoleComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
    }

    private void OnComponentInit(Entity<LibraryConsoleComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent, BookSlotId, ent.Comp.BookSlot);
    }

    private void OnComponentRemove(Entity<LibraryConsoleComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.BookSlot);
    }

    private void OnSlotChanged(EntityUid uid, LibraryConsoleComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID != component.BookSlot.ID)
            return;

        UpdateUiState((uid, component));
    }

    private void OnUiOpened(Entity<LibraryConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnUploadBook(Entity<LibraryConsoleComponent> ent, ref LibraryConsoleUploadBookMessage args)
    {
        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        if (string.IsNullOrWhiteSpace(args.Title) ||
            string.IsNullOrWhiteSpace(args.Author) ||
            string.IsNullOrWhiteSpace(args.Content))
        {
            _audio.PlayPvs(ent.Comp.ErrorSound, ent.Owner);
            return;
        }

        var title = args.Title;
        var author = args.Author;
        var content = args.Content;
        var ckey = session.Name;
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);
        _ = AddBookAsync(ent, title, author, content, date, ckey);
    }

    private async Task AddBookAsync(Entity<LibraryConsoleComponent> ent, string title, string author, string content, string date, string ckey)
    {
        var server = await _serverDbEntry.ServerEntity;
        await _dbManager.AddLibraryBookAsync(server.Id, title, author, content, date, ckey);

        // Refresh the UI so the browse tab shows the newly uploaded book.
        if (EntityManager.EntityExists(ent))
            UpdateUiState(ent);
    }

    private void OnDownloadBook(Entity<LibraryConsoleComponent> ent, ref LibraryConsoleDownloadBookMessage args)
    {
        _ = DownloadBookAsync(ent, args.BookId);
    }

    private async Task DownloadBookAsync(Entity<LibraryConsoleComponent> ent, int bookId)
    {
        var server = await _serverDbEntry.ServerEntity;
        var books = await _dbManager.GetLibraryBooksAsync(server.Id);
        var book = books.FirstOrDefault(b => b.Id == bookId);

        if (book == null || !EntityManager.EntityExists(ent))
            return;

        if (ent.Comp.BookSlot.Item is not { } bookEntity ||
            !TryComp<PaperComponent>(bookEntity, out var paper))
        {
            _audio.PlayPvs(ent.Comp.ErrorSound, ent.Owner);
            return;
        }

        paper.Content = book.Content;
        _metaData.SetEntityName(bookEntity, book.Title);
        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);

        // Refresh UI so the inserted book's content field stays in sync.
        UpdateUiState(ent);
    }

    private void OnPowerChanged(Entity<LibraryConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            _ui.CloseUi(ent.Owner, LibraryConsoleUiKey.Key);
    }

    private void UpdateUiState(Entity<LibraryConsoleComponent> ent)
    {
        _ = UpdateUiStateAsync(ent);
    }

    private async Task UpdateUiStateAsync(Entity<LibraryConsoleComponent> ent)
    {
        var server = await _serverDbEntry.ServerEntity;
        var books = await _dbManager.GetLibraryBooksAsync(server.Id);

        if (!EntityManager.EntityExists(ent))
            return;

        string? bookContent = null;
        if (ent.Comp.BookSlot.Item is { } bookEntity &&
            TryComp<PaperComponent>(bookEntity, out var paper))
        {
            bookContent = paper.Content;
        }

        var bookData = books
            .Select(b => new LibraryBookData(b.Id, b.Title, b.Author, b.Date))
            .ToList();

        var state = new LibraryConsoleBoundUserInterfaceState(enabled: true, bookContent, bookData);
        _ui.SetUiState(ent.Owner, LibraryConsoleUiKey.Key, state);
    }
}

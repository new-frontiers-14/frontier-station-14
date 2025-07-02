using System.Linq;
using Content.Shared._Goobstation.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._Goobstation.Research.UI;

// Frontier: renamed from FancyResearchConsoleBoundUserInterface to ResearchConsoleGoobBoundUserInterface to avoid collisions (see RT#5648)
[UsedImplicitly]
public sealed class ResearchConsoleGoobBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FancyResearchConsoleMenu? _consoleMenu;  // Goobstation R&D Console rework - ResearchConsoleMenu -> FancyResearchConsoleMenu

    // Frontier
    private SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Sound to play when unlocking a technology
    private static readonly SoundPathSpecifier UnlockSound = new("/Audio/_NF/Research/unlock.ogg");

    public ResearchConsoleGoobBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _audioSystem = EntMan.System<SharedAudioSystem>();

        _sawmill = _logManager.GetSawmill("research.console");
        _sawmill.Debug($"ResearchConsoleGoobBoundUserInterface created for {owner} with key {uiKey}");
    }
    // End Frontier

    protected override void Open()
    {
        base.Open();

        var owner = Owner;
        _sawmill.Debug($"Opening UI for {owner}"); // Frontier: added debug log

        _consoleMenu = this.CreateWindow<FancyResearchConsoleMenu>();   // Goobstation R&D Console rework - ResearchConsoleMenu -> FancyResearchConsoleMenu
        _consoleMenu.SetEntity(owner);
        _consoleMenu.OnClose += () => _consoleMenu = null;

        // Set up technology unlock handler
        _consoleMenu.OnTechnologyCardPressed += id =>
        {
            try
            {
                _sawmill.Debug($"Sending ConsoleUnlockTechnologyMessage for tech ID: {id}"); // Frontier: added debug log

                // Create and send the message
                var message = new ConsoleUnlockTechnologyMessage(id);
                SendMessage(message);

                _audioSystem.PlayPvs(UnlockSound, owner, AudioParams.Default); // Frontier: Play unlock sound - client-side only

                _sawmill.Info($"Sent unlock message for technology: {id}"); // Frontier: Log success
            }
            catch (Exception ex) // Frontier: Log any exceptions that occur during message sending
            {
                _sawmill.Error($"Error sending technology unlock message for {id}: {ex}");
            }
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            _sawmill.Debug("Sending ConsoleServerSelectionMessage"); // Frontier: added debug log
            SendMessage(new ConsoleServerSelectionMessage());
        };
    }

    public override void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        base.OnProtoReload(args);

        if (!args.WasModified<TechnologyPrototype>())
            return;

        if (State is not ResearchConsoleBoundInterfaceState rState)
            return;

        _sawmill.Debug("Reloading prototypes in UI"); // Frontier: added debug log
        _consoleMenu?.UpdatePanels(rState.Researches);
        _consoleMenu?.UpdateInformationPanel(rState.Points);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
        {
            _sawmill.Warning("Received non-ResearchConsoleBoundInterfaceState state"); // Frontier: added debug log
            return;
        }

        // Goobstation checks added
        // Thats for avoiding refresh spam when only points are updated
        if (_consoleMenu == null)
        {
            _sawmill.Warning("Console menu is null during state update"); // Frontier: added debug log
            return;
        }

        _sawmill.Debug($"Updating UI state with {castState.Points} points and {castState.Researches.Count} technologies"); // Frontier: added debug log

        var availableTechs = castState.Researches.Count(t => t.Value == ResearchAvailability.Available);
        _sawmill.Debug($"Available technologies: {availableTechs}"); // Frontier: added debug log

        if (!_consoleMenu.List.SequenceEqual(castState.Researches))
        {
            _sawmill.Debug("Technologies list changed, updating panels"); // Frontier: added debug log
            _consoleMenu.UpdatePanels(castState.Researches);
        }

        _consoleMenu.UpdateInformationPanel(castState.Points); // Frontier: always update panel
    }
}

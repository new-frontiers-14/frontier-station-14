using System.Linq;
using Content.Shared._NF.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Research.UI;

[UsedImplicitly]
public sealed class ResearchConsoleFrontierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FancyResearchConsoleMenu? _consoleMenu;

    private SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Sound to play when unlocking a technology
    private static readonly SoundPathSpecifier UnlockSound = new("/Audio/_NF/Research/unlock.ogg");

    public ResearchConsoleFrontierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _audioSystem = EntMan.System<SharedAudioSystem>();

        _sawmill = _logManager.GetSawmill("research.console");
        _sawmill.Debug($"ResearchConsoleFrontierBoundUserInterface created for {owner} with key {uiKey}");
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner;
        _sawmill.Debug($"Opening UI for {owner}");

        _consoleMenu = this.CreateWindow<FancyResearchConsoleMenu>();
        _consoleMenu.SetEntity(owner);
        _consoleMenu.OnClose += () => _consoleMenu = null;

        // Set up technology unlock handler
        _consoleMenu.OnTechnologyCardPressed += id =>
        {
            try
            {
                _sawmill.Debug($"Sending ConsoleUnlockTechnologyMessage for tech ID: {id}");

                // Create and send the message
                var message = new ConsoleUnlockTechnologyMessage(id);
                SendMessage(message);

                _audioSystem.PlayPvs(UnlockSound, owner, AudioParams.Default); // Play unlock sound - client-side only

                _sawmill.Info($"Sent unlock message for technology: {id}"); // Log success
            }
            catch (Exception ex) // Log any exceptions that occur during message sending
            {
                _sawmill.Error($"Error sending technology unlock message for {id}: {ex}");
            }
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            _sawmill.Debug("Sending ConsoleServerSelectionMessage");
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

        _sawmill.Debug("Reloading prototypes in UI");
        _consoleMenu?.UpdatePanels(rState.Researches);
        _consoleMenu?.UpdateInformationPanel(rState.Points);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
        {
            _sawmill.Warning("Received non-ResearchConsoleBoundInterfaceState state");
            return;
        }

        // Thats for avoiding refresh spam when only points are updated
        if (_consoleMenu == null)
        {
            _sawmill.Warning("Console menu is null during state update");
            return;
        }

        _sawmill.Debug($"Updating UI state with {castState.Points} points and {castState.Researches.Count} technologies");

        var availableTechs = castState.Researches.Count(t => t.Value == ResearchAvailability.Available);
        _sawmill.Debug($"Available technologies: {availableTechs}");

        if (!_consoleMenu.List.SequenceEqual(castState.Researches))
        {
            _sawmill.Debug("Technologies list changed, updating panels");
            _consoleMenu.UpdatePanels(castState.Researches);
        }

        _consoleMenu.UpdateInformationPanel(castState.Points); // always update panel
    }
}

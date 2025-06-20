// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <logkedr18@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client._Goobstation.Research;
using Content.Shared._Goobstation.Research; // Make sure this import is present
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Client._Goobstation.Research.UI;

// Frontier: renamed from FancyResearchConsoleBoundUserInterface to ResearchConsoleGoobBoundUserInterface to avoid collisions (see RT#5648)
[UsedImplicitly]
public sealed class ResearchConsoleGoobBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FancyResearchConsoleMenu? _consoleMenu;  // Goobstation R&D Console rework - ResearchConsoleMenu -> FancyResearchConsoleMenu

    public ResearchConsoleGoobBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        Logger.Debug($"[Research] ResearchConsoleGoobBoundUserInterface created for {owner} with key {uiKey}");
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner;
        Logger.Debug($"[Research] Opening UI for {owner}");

        _consoleMenu = this.CreateWindow<FancyResearchConsoleMenu>();   // Goobstation R&D Console rework - ResearchConsoleMenu -> FancyResearchConsoleMenu
        _consoleMenu.SetEntity(owner);
        _consoleMenu.OnClose += () => _consoleMenu = null;

        // Set up technology unlock handler
        _consoleMenu.OnTechnologyCardPressed += id =>
        {
            try
            {
                Logger.Debug($"[Research] Sending ConsoleUnlockTechnologyMessage for tech ID: {id}");
                
                // Create and send the message
                var message = new ConsoleUnlockTechnologyMessage(id);
                SendMessage(message);
                
                // Log success
                Logger.Info($"[Research] Sent unlock message for technology: {id}");
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during message sending
                Logger.Error($"[Research] Error sending technology unlock message for {id}: {ex}");
            }
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            Logger.Debug("[Research] Sending ConsoleServerSelectionMessage");
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

        Logger.Debug("[Research] Reloading prototypes in UI");
        _consoleMenu?.UpdatePanels(rState.Researches);
        _consoleMenu?.UpdateInformationPanel(rState.Points);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
        {
            Logger.Warning("[Research] Received non-ResearchConsoleBoundInterfaceState state");
            return;
        }

        // Goobstation checks added
        // Thats for avoiding refresh spam when only points are updated
        if (_consoleMenu == null)
        {
            Logger.Warning("[Research] Console menu is null during state update");
            return;
        }
        
        Logger.Debug($"[Research] Updating UI state with {castState.Points} points and {castState.Researches.Count} technologies");
        
        var availableTechs = castState.Researches.Count(t => t.Value == ResearchAvailability.Available);
        Logger.Debug($"[Research] Available technologies: {availableTechs}");
        
        if (!_consoleMenu.List.SequenceEqual(castState.Researches))
        {
            Logger.Debug("[Research] Technologies list changed, updating panels");
            _consoleMenu.UpdatePanels(castState.Researches);
        }
        
        _consoleMenu.UpdateInformationPanel(castState.Points); // Frontier: always update panel
    }
}

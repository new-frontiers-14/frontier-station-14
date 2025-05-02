using Content.Client.Computer;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;

namespace Content.Client.Shuttles.UI;

public sealed partial class RadarConsoleWindow : FancyWindow,
    IComputerWindow<NavInterfaceState>
{
    public void SetConsole(EntityUid consoleEntity)
    {
        RadarScreen.SetConsole(consoleEntity);
    }
}

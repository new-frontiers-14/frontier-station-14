using Content.Shared._NF.PlantAnalyzer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.PlantAnalyzer.UI;

[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PlantAnalyzerWindow? _window;

    public PlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        if (_window == null)
        {
            _window = this.CreateWindowCenteredLeft<PlantAnalyzerWindow>();
            _window.Title = Loc.GetString("plant-analyzer-interface-title");
            _window.OnAdvancedModeChanged += AdvPressed;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not PlantAnalyzerBoundUserInterfaceState cast)
            return;
        _window.UpdateState(cast);
    }

    public void AdvPressed(bool advancedScanMode)
    {
        SendMessage(new PlantAnalyzerSetMode(advancedScanMode));
    }
}

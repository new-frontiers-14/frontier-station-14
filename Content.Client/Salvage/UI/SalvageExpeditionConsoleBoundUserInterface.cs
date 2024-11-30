using Content.Shared.Salvage.Expeditions;
using JetBrains.Annotations;

namespace Content.Client.Salvage.UI;

[UsedImplicitly]
public sealed class SalvageExpeditionConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SalvageExpeditionWindow? _window;

    public SalvageExpeditionConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new SalvageExpeditionWindow();
        _window.ClaimMission += index =>
        {
            SendMessage(new ClaimSalvageMessage()
            {
                Index = index,
            });
        };
        _window.FinishMission += () => SendMessage(new FinishSalvageMessage()); // Frontier
        _window.OnClose += Close;
        _window?.OpenCenteredLeft();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
        _window = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageExpeditionConsoleState current)
            return;

        _window?.UpdateState(current);
    }
}

using Content.Shared.Salvage.Expeditions;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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
        _window = this.CreateWindowCenteredLeft<SalvageExpeditionWindow>(); // Frontier: OfferingWindow<SalvageExpeditionWindow
        _window.Title = Loc.GetString("salvage-expedition-window-title");
        // Frontier: handlers
        _window.ClaimMission += index =>
        {
            SendMessage(new ClaimSalvageMessage()
            {
                Index = index,
            });
        };
        _window.FinishMission += () => SendMessage(new FinishSalvageMessage());
        // End Frontier
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageExpeditionConsoleState current)
            return;

        _window?.UpdateState(current);
    }
}

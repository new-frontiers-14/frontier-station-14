using Content.Shared._DV.CustomObjectiveSummary;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;

namespace Content.Client._DV.CustomObjectiveSummary;

public sealed class CustomObjectiveSummaryUIController : UIController
{
    [Dependency] private readonly IClientNetManager _net = default!;

    private CustomObjectiveSummaryWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CustomObjectiveSummaryOpenMessage>(OnCustomObjectiveSummaryOpen);
    }

    private void OnCustomObjectiveSummaryOpen(CustomObjectiveSummaryOpenMessage msg, EntitySessionEventArgs args)
    {
        OpenWindow();
    }

    public void OpenWindow()
    {
        _window ??= new CustomObjectiveSummaryWindow();

        if (_window.IsOpen)
            return;

        _window.OpenCentered();
        _window.OnSubmitted += OnFeedbackSubmitted;
    }

    private void OnFeedbackSubmitted(string args)
    {
        var msg = new CustomObjectiveClientSetObjective
        {
            Summary = args,
        };
        _net.ClientSendMessage(msg);
        _window?.Close();
    }
}

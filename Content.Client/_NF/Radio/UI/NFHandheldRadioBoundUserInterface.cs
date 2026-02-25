using Content.Shared._NF.Radio;
using JetBrains.Annotations;

namespace Content.Client._NF.Radio.UI;


[UsedImplicitly]
public sealed class NFHandheldRadioBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NFHandheldRadioMenu? _menu;

    public NFHandheldRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnMicPressed += mode =>
        {
            SendMessage(new SetHandheldRadioMicModeMessage(mode));
        };
        _menu.OnSpeakerPressed += mode =>
        {
            SendMessage(new SetHandheldRadioSpeakerModeMessage(mode));
        };
        _menu.OnFrequencyChanged += frequency =>
        {
            if (int.TryParse(frequency.Trim(), out var intFreq) && intFreq > 0)
                SendMessage(new Content.Shared._NC.Radio.SelectHandheldRadioFrequencyMessage(intFreq));
            else
                SendMessage(new Content.Shared._NC.Radio.SelectHandheldRadioFrequencyMessage(-1)); // Query the current frequency
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not HandheldRadioBoundUIState msg)
            return;

        _menu?.Update(msg);
    }
}

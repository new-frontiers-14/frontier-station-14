using Content.Server._NF.Radio.Components;
using Content.Server.Radio.Components;
using Content.Shared._NF.Radio;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Radio.Systems;

public sealed partial class HandheldRadioSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // Minimum, maximum radio frequencies
    private const int MinRadioFrequency = 1000;
    private const int MaxRadioFrequency = 3000;

    private void InitializeInteract()
    {
        SubscribeLocalEvent<HandheldRadioComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HandheldRadioComponent, BeforeActivatableUIOpenEvent>(OnBeforeHandheldRadioUiOpen);

        SubscribeLocalEvent<HandheldRadioComponent, SetHandheldRadioMicModeMessage>(OnSetHandheldRadioMicMode);
        SubscribeLocalEvent<HandheldRadioComponent, SetHandheldRadioSpeakerModeMessage>(OnSetHandheldRadioSpeakerMode);
        SubscribeLocalEvent<HandheldRadioComponent, SelectHandheldRadioFrequencyMessage>(OnChangeHandheldRadioFrequency);
    }

    private void OnExamine(EntityUid uid, HandheldRadioComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _protoMan.Index<RadioChannelPrototype>(component.Channel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine", ("frequency", component.Frequency!.Value)));
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void OnBeforeHandheldRadioUiOpen(Entity<HandheldRadioComponent> radio, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateHandheldRadioUi(radio);
    }

    private void OnSetHandheldRadioMicMode(Entity<HandheldRadioComponent> radio, ref SetHandheldRadioMicModeMessage args)
    {
        if (!args.Actor.Valid)
            return;

        radio.Comp.MicrophoneMode = args.Mode;
        UpdateHandheldRadioUi(radio);
    }

    private void OnSetHandheldRadioSpeakerMode(Entity<HandheldRadioComponent> radio, ref SetHandheldRadioSpeakerModeMessage args)
    {
        if (!args.Actor.Valid)
            return;

        radio.Comp.SpeakerMode = args.Mode;
        UpdateHandheldRadioUi(radio);
    }

    private void OnChangeHandheldRadioFrequency(Entity<HandheldRadioComponent> radio, ref SelectHandheldRadioFrequencyMessage args)
    {
        if (!args.Actor.Valid)
            return;

        // Update frequency if valid and within range.
        if (args.Frequency >= MinRadioFrequency && args.Frequency <= MaxRadioFrequency)
            radio.Comp.Frequency = args.Frequency;

        // Update UI with current frequency.
        UpdateHandheldRadioUi(radio);
    }

    private void UpdateHandheldRadioUi(Entity<HandheldRadioComponent> radio)
    {
        var frequency = radio.Comp.Frequency!.Value;
        var micState = radio.Comp.MicrophoneMode;
        var speakerState = radio.Comp.SpeakerMode;

        var state = new HandheldRadioBoundUIState(micState, speakerState, frequency);
        if (TryComp<UserInterfaceComponent>(radio, out var uiComp))
            _ui.SetUiState((radio.Owner, uiComp), HandheldRadioUiKey.Key, state);
    }
}

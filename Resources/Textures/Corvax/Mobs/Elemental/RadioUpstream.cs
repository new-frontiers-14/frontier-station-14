    // Corvax-Frontier-Start
    #region Handheld Radio

    private void OnBeforeHandheldRadioUiOpen(EntityUid uid, RadioMicrophoneComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateHandheldRadioUi(uid, component);
    }

    private void OnToggleHandheldRadioMic(EntityUid uid, RadioMicrophoneComponent component, ToggleHandheldRadioMicMessage args)
    {
        SetMicrophoneEnabled(uid, args.Actor, args.Enabled, true, component);
        UpdateHandheldRadioUi(uid, component);
    }

    private void OnToggleHandheldRadioSpeaker(EntityUid uid, RadioMicrophoneComponent component, ToggleHandheldRadioSpeakerMessage args)
    {
        if (!TryComp<RadioSpeakerComponent>(uid, out var speakerComponent))
            return;

        SetSpeakerEnabled(uid, args.Actor, args.Enabled, true, speakerComponent);
        UpdateHandheldRadioUi(uid, component);
    }

    private void OnChangeHandheldRadioFrequency(EntityUid uid, RadioMicrophoneComponent component, SelectHandheldRadioFrequencyMessage args)
    {
        component.Frequency = args.Frequency;
        UpdateHandheldRadioUi(uid, component);
    }

    private void UpdateHandheldRadioUi(EntityUid uid, RadioMicrophoneComponent component)
    {
        if (!TryComp<RadioSpeakerComponent>(uid, out var speakerComponent))
            speakerComponent = null;

        var micEnabled = component.Enabled;
        var speakerEnabled = speakerComponent?.Enabled ?? false;
        var frequency = component.Frequency;
        var state = new HandheldRadioBoundUIState(micEnabled, speakerEnabled, frequency);
        _ui.SetUiState(uid, HandheldRadioUiKey.Key, state);
    }

    #endregion
    // Corvax-Frontier-End
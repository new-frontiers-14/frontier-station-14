using Content.Client.Chat.TypingIndicator;
using Content.Shared._DV.AACTablet;
using Content.Shared._DV.QuickPhrase;
using Content.Shared.Chat.TypingIndicator;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._DV.AACTablet.UI;

public sealed class AACBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AACWindow? _window;

    private static readonly ProtoId<TypingIndicatorPrototype> AACTypingIndicator = "aac";

    private TypingIndicatorSystem? _typing;

    public AACBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (_window is { Disposed: false })
            _window.Close();

        _window = this.CreateWindow<AACWindow>();
        _window.OnClose += Close;
        _window.PhraseButtonPressed += OnPhraseButtonPressed;
        _window.Typing += OnTyping;
        _window.SubmitPressed += OnSubmit;
    }

    private void OnPhraseButtonPressed(List<ProtoId<QuickPhrasePrototype>> phraseId, string prefix)
    {
        SendMessage(new AACTabletSendPhraseMessage(phraseId, prefix));
    }

    private void OnTyping()
    {
        _typing ??= EntMan.System<TypingIndicatorSystem>();
        _typing?.ClientAlternateTyping(AACTypingIndicator);
    }

    private void OnSubmit()
    {
        _typing ??= EntMan.System<TypingIndicatorSystem>();
        _typing?.ClientSubmittedChatText();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AACTabletBuiState msg)
            return;

        _window?.Update(msg);
    }
}

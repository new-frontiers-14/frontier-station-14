using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Shared._NF.Paper.PaperBundleComponent;

namespace Content.Client._NF.Paper.UI;

[UsedImplicitly]
public sealed class PaperBundleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperBundleWindow? _window;

    public PaperBundleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperBundleWindow>();
        _window.OnSaved += OnInputText;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaperBundleBoundUserInterfaceState bundleState)
            _window?.Populate(bundleState);
    }

    private void OnInputText(NetEntity pageEntity, string text)
    {
        SendMessage(new PaperBundleInputTextMessage(pageEntity, text));
    }
}

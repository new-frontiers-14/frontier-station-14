using Content.Shared.Language;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Language;

[UsedImplicitly]
public sealed class LanguageMenuUserInterface : BoundUserInterface
{
    private LanguageMenuWindow? _window;

    public LanguageMenuUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new LanguageMenuWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SharedLanguageSystem.LanguageMenuState menuState)
        {
            _window?.UpdateState(menuState);
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}

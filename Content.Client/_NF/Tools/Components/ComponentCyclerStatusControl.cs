using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared._NF.Item.ItemToggle.Components;
using Content.Shared.Tools.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._NF.Tools.Components;

public sealed class ComponentCyclerStatusControl : Control
{
    private readonly ComponentCyclerComponent _parent;
    private readonly RichTextLabel _label;

    public ComponentCyclerStatusControl(ComponentCyclerComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        _label.SetMarkup(_parent.StatusShowBehavior ? _parent.Entries[_parent.CurrentEntry].QualityName : string.Empty);
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_parent.UiUpdateNeeded)
        {
            _parent.UiUpdateNeeded = false;
            Update();
        }
    }

    public void Update()
    {
        _label.SetMarkup(_parent.StatusShowBehavior ? _parent.Entries[_parent.CurrentEntry].QualityName : string.Empty);
    }
}

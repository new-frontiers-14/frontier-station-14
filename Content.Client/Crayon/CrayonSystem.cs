using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Paper;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    // Didn't do in shared because I don't think most of the server stuff can be predicted.
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentHandleState>(OnCrayonHandleState);
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));
    }

    private void OnCrayonHandleState(EntityUid uid, CrayonComponent component, ref ComponentHandleState args) // Frontier: remove static
    {
        if (args.Current is not CrayonComponentState state) return;

        component.Color = state.Color;
        component.SelectedState = state.State;
        component.Charges = state.Charges;
        component.Capacity = state.Capacity;

        component.UIUpdateNeeded = true;

        // Frontier: ensure signature colour is consistent
        if (TryComp<StampComponent>(uid, out var stamp))
        {
            stamp.StampedColor = state.Color;
        }
        // End Frontier
    }

    private sealed class StatusControl : Control
    {
        private readonly CrayonComponent _parent;
        private readonly RichTextLabel _label;

        public StatusControl(CrayonComponent parent)
        {
            _parent = parent;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);

            parent.UIUpdateNeeded = true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_parent.UIUpdateNeeded)
            {
                return;
            }

            _parent.UIUpdateNeeded = false;

            // Frontier: unlimited crayon
            if (_parent.Capacity == int.MaxValue)
            {
                _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label-unlimited",
                    ("color", _parent.Color),
                    ("state", _parent.SelectedState)));
                return;
            }
            // End Frontier

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                ("color",_parent.Color),
                ("state",_parent.SelectedState),
                ("charges", _parent.Charges),
                ("capacity",_parent.Capacity)));
        }
    }
}

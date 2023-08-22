using System.Numerics;
using Content.Shared._NF.Bodycam;
//using Content.Shared._NF.Bodycam.Component;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._NF.Bodycam.Components;

public sealed class BodycamStatus : Control
{
    private const float TimerCycle = 1;

    private readonly BodycamComponent _parent;
    private readonly PanelContainer[] _sections = new PanelContainer[BodycamComponent.StatusLevels - 1];

    private float _timer;

    private static readonly StyleBoxFlat StyleBoxLit = new()
    {
        BackgroundColor = Color.LimeGreen
    };

    private static readonly StyleBoxFlat StyleBoxUnlit = new()
    {
        BackgroundColor = Color.Black
    };

    public BodycamStatus(BodycamComponent parent)
    {
        _parent = parent;

        var wrapper = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            HorizontalAlignment = HAlignment.Center
        };

        AddChild(wrapper);

        for (var i = 0; i < _sections.Length; i++)
        {
            var panel = new PanelContainer {MinSize = new Vector2(20, 20)};
            wrapper.AddChild(panel);
            _sections[i] = panel;
        }
    }
}

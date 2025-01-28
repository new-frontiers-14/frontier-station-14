using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using static Content.Shared._NF.CryoSleep.SharedCryoSleepSystem;

namespace Content.Client._NF.CryoSleep;

public sealed class CryosleepWakeupWindow : DefaultWindow, IEntityEventSubscriber
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public RichTextLabel Label;
    public Button DenyButton;
    public Button AcceptButton;

    public CryosleepWakeupWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("cryo-wakeup-window-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        (Label = new RichTextLabel()
                        {
                            HorizontalExpand = true,
                            MaxWidth = 300,
                            StyleClasses = {  }
                        }),
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            Align = BoxContainer.AlignMode.Center,
                            Children =
                            {
                                (AcceptButton = new Button
                                {
                                    Text = Loc.GetString("cryo-wakeup-window-accept-button"),
                                }),

                                (new Control()
                                {
                                    MinSize = new Vector2i(20, 0)
                                }),

                                (DenyButton = new Button
                                {
                                    Text = Loc.GetString("cryo-wakeup-window-deny-button"),
                                })
                            }
                        },
                    }
                },
            }
        });

        _entityManager.EventBus.SubscribeEvent<WakeupRequestMessage.Response>(EventSource.Network, this, OnWakeupResponse);
        DenyButton.OnPressed += _ => Close();
        AcceptButton.OnPressed += _ => OnAccept();
    }

    protected override void Opened()
    {
        Label.SetMessage(Loc.GetString("cryo-wakeup-window-rules"));
        DenyButton.Disabled = false;
        AcceptButton.Disabled = false;
    }

    private void OnAccept()
    {
        var message = new WakeupRequestMessage();
        _entityManager.EntityNetManager?.SendSystemNetworkMessage(message);

        // Disable the buttons to make the user wait for a response
        AcceptButton.Disabled = true;
        DenyButton.Disabled = true;
    }

    private void OnWakeupResponse(WakeupRequestMessage.Response response)
    {
        if (response.Status == ReturnToBodyStatus.Success)
        {
            Close();
            return;
        }

        // Failure
        DenyButton.Disabled = false;
        if (response.Status == ReturnToBodyStatus.Occupied)
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-occupied"));
        else if (response.Status == ReturnToBodyStatus.NoCryopodAvailable)
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-no-cryopod"));
        else if (response.Status == ReturnToBodyStatus.BodyMissing)
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-no-body"));
        else if (response.Status == ReturnToBodyStatus.Disabled)
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-disabled"));
    }
}

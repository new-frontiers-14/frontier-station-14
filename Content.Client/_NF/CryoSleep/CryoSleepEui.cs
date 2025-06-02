using Content.Client.Eui;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.CryoSleep;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Systems;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._NF.CryoSleep;

[UsedImplicitly]
public sealed class CryoSleepEui : BaseEui
{
    private readonly AcceptCryoWindow _window;

    public CryoSleepEui()
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var playerEntity = IoCManager.Resolve<ISharedPlayerManager>().LocalEntity;

        _window = new AcceptCryoWindow();

        // Try to get the player's mind.
        if (!entityManager.TryGetComponent(playerEntity, out JobTrackingComponent? jobTracking)
            || jobTracking.Job == null
            || !SharedJobTrackingSystem.JobShouldBeReopened(jobTracking.Job.Value))
        {
            var configManager = IoCManager.Resolve<INetConfigurationManager>();
            var cryoTime = TimeSpan.FromSeconds(configManager.GetCVar(NFCCVars.CryoExpirationTime));
            _window.StoreText.Text = Loc.GetString("accept-cryo-window-prompt-stored", ("time", cryoTime));
        }
        else
        {
            _window.StoreText.Text = Loc.GetString("accept-cryo-window-prompt-not-stored");
        }

        _window.OnAccept += () =>
        {
            SendMessage(new AcceptCryoChoiceMessage(AcceptCryoUiButton.Accept));
            _window.Close();
        };

        _window.OnDeny += () =>
        {
            SendMessage(new AcceptCryoChoiceMessage(AcceptCryoUiButton.Deny));
            _window.Close();
        };
    }

    public override void Opened()
    {
        base.Opened();

        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window.Close();
    }
}

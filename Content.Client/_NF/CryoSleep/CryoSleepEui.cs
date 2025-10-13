using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.CartridgeLoader.Cartridges;
using Content.Client.Eui;
using Content.Client.Inventory;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.CryoSleep;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Components;
using Content.Shared.Eui;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Storage;
using Content.Shared.Store.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;


namespace Content.Client._NF.CryoSleep;

[UsedImplicitly]
public sealed class CryoSleepEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly AcceptCryoWindow _window;
    private EntityUid? _playerEntity = IoCManager.Resolve<ISharedPlayerManager>().LocalEntity;
    public CryoSleepEui()
    {


        _window = new AcceptCryoWindow();

        // Try to get the player's mind.
        if (!_entityManager.TryGetComponent(_playerEntity, out JobTrackingComponent? jobTracking)
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

    private static readonly Color WarningColor = Color.Yellow;

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is CryoSleepWarningMessage warningMsg)
        {
            //I don't anticipate this check ever failing, but it's better to visually fail than silently fail or throw
            if (!_entityManager.TryGetComponent<InventorySlotsComponent>(_playerEntity, out var slotsComp))
            {
                _window.UplinkWarningText.Text = Loc.GetString("accept-cryo-window-prompt-unable-to-scan");
                return;
            }
            //Shuttle
            string? shuttleWarningLoc = GetShuttleWarningLocMessage(warningMsg.ShuttleOnPDA,
                warningMsg.FoundMoreShuttles,
                warningMsg.InventoryShuttleDeed,
                slotsComp);
            //Uplink
            string? uplinkWarningLoc = warningMsg.FoundUplink.HasValue
                ? GetUplinkWarningLocMessage(warningMsg.FoundUplink.Value, slotsComp)
                : null;
            //Items
            string? itemWarningLoc = GetImportantItemWarningLocMessage(warningMsg.ImportantItems, slotsComp);

            if (shuttleWarningLoc != null)
            {
                _window.ShuttleWarningText.SetMessage(shuttleWarningLoc, null, WarningColor);
            }

            if (uplinkWarningLoc != null)
            {
                _window.UplinkWarningText.SetMessage(uplinkWarningLoc, null, WarningColor);
            }

            if (itemWarningLoc != null)
            {
                _window.ItemWarningText.SetMessage(itemWarningLoc, null, WarningColor);
            }
        }
    }

    private string GetStorageName(CryoSleepWarningMessage.NetworkedWarningItem item, InventorySlotsComponent inventoryComp)
    {
        if (item.SlotId == null)
        {
            return Identity.Name(_entityManager.GetEntity(item.Container!.Value), _entityManager);
        }
        else
        {
            //Lowercase this just to make the name not look weird in the popup
            var returnVal = inventoryComp.SlotData[item.SlotId].SlotDisplayName;
            //I can't execute without assigning it first
            return returnVal.ToLower();
        }
    }

    //All of these message get methods were moved to be separate to make the code less rigid, and easier to read.

    //Grab any needed shuttle warnings.
    //Returns null if no warning is needed
    private string? GetShuttleWarningLocMessage(bool hasShuttleOnPDA,
        bool foundMoreShuttles,
        CryoSleepWarningMessage.NetworkedWarningItem? foundShuttleDeed,
        InventorySlotsComponent slotsComp)
    {

        if (foundShuttleDeed.HasValue)
        {
            var localDeed = _entityManager.GetEntity(foundShuttleDeed.Value.Item);
            //Get either the name of the item the deed is in, or the id of the slot.
            var storageName = GetStorageName(foundShuttleDeed.Value, slotsComp);
            string key;
            //Four different messages for four different cases, thankfully they all have the same blank spots
            if (hasShuttleOnPDA)
            {
                key = foundMoreShuttles
                    ? "accept-cryo-window-prompt-shuttle-pda-and-many-bag-warning"
                    : "accept-cryo-window-prompt-shuttle-pda-and-bag-warning";
            }
            else
            {
                key = foundMoreShuttles
                    ? "accept-cryo-window-prompt-shuttle-many-bag-warning"
                    : "accept-cryo-window-prompt-shuttle-bag-warning";
            }

            return Loc.GetString(key,
                ("deed", Identity.Name(localDeed, _entityManager)),
                ("storage", storageName));
        }

        //No shuttle in their bag, so all we need to do is warn if its in the PDA
        return hasShuttleOnPDA
            ? Loc.GetString("accept-cryo-window-prompt-shuttle-pda-warning")
            : null;
    }

    //Grab any needed item warnings.
    //Returns null if no warning is needed
    private string? GetImportantItemWarningLocMessage(
        List<CryoSleepWarningMessage.NetworkedWarningItem> warningItemsList,
        InventorySlotsComponent slotsComp)
    {
        switch (warningItemsList.Count)
        {
            case 0:
                return null;
            case 1:
            {
                var item = warningItemsList[0];
                var storageName = GetStorageName(item, slotsComp);
                return Loc.GetString("accept-cryo-window-prompt-one-item-warning",
                    ("item", Identity.Name(_entityManager.GetEntity(item.Item), _entityManager)),
                    ("storage", storageName));
            }
            case >= 2:
            {
                var item1 = warningItemsList[0];
                var storageName1 = GetStorageName(item1, slotsComp);
                var item2 = warningItemsList[1];
                var storageName2 = GetStorageName(item2, slotsComp);
                //This branch both functions for 2 and more than two items, the only difference is which localization key to use.
                //To save copy and pasting, I just pick the key here.
                var key = warningItemsList.Count > 2
                    ? "accept-cryo-window-prompt-many-items-warning"
                    : "accept-cryo-window-prompt-two-items-warning";

                return Loc.GetString(key,
                    ("item1", Identity.Name(_entityManager.GetEntity(item1.Item), _entityManager)),
                    ("storage1", storageName1),
                    ("item2", Identity.Name(_entityManager.GetEntity(item2.Item), _entityManager)),
                    ("storage2", storageName2),
                    //This key is only needed if there are 3+ items(since the two-items key doesn't have this blank),
                    //but putting it in doesn't break anything if the key doesn't have this blank in it.
                    ("num-extra-items", warningItemsList.Count - 2));
            }
            default:
                //Unreachable statement, but gotta make the complier happy /shrug
                throw new Exception("warningItemsList.Count was somehow less than 0 in CryoSleepEui.cs.");
        }

    }

    //Grab any needed uplink warnings.
    //Returns null if no warning is needed
    private string? GetUplinkWarningLocMessage(CryoSleepWarningMessage.NetworkedWarningItem foundUplink,
        InventorySlotsComponent slotsComp)
    {
        var localUplink = _entityManager.GetEntity(foundUplink.Item);
        if (!_entityManager.TryGetComponent<StoreComponent>(localUplink, out var store))
            return null;
        var currencyProtoId = store.Balance.Keys.First();
        var amount = store.Balance[currencyProtoId];
        if (amount == 0
            || !_prototypeManager.TryIndex(currencyProtoId, out var currencyProto))
            return null;
        return Loc.GetString("accept-cryo-window-prompt-uplink-warning",
            ("uplink", Identity.Name(_entityManager.GetEntity(foundUplink.Item), _entityManager)),
            ("storage", GetStorageName(foundUplink, slotsComp)),
            ("amount", amount),
            ("currency",  Loc.GetString(currencyProto.DisplayName)));
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

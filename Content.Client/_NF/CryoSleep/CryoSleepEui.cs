using System.Diagnostics.CodeAnalysis;
using System.Linq;
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


namespace Content.Client._NF.CryoSleep;

[UsedImplicitly]
public sealed class CryoSleepEui : BaseEui
{
    //Broken :(
    //TODO: Find a replacement for the broken _inventorySystem
    //[Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
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
            //I don't anticipate this check ever failing, but its better to visually fail than silently fair or throw
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

    //TODO: Write a method to "prettyify" the ID names
    //TODO: Ask around to see if anyone knows if you can get localized names of item slots
    string GetStorageName(CryoSleepWarningMessage.NetworkedWarningItem item, InventorySlotsComponent inventoryComp)
    {
        if (item.SlotId == null)
        {
            return Identity.Name(_entityManager.GetEntity(item.Container!.Value), _entityManager);
        }
        else
        {
            return inventoryComp.SlotData[item.SlotId].SlotDisplayName;
        }
    }


    //The if statement was too hard to read, so I moved it to its own method where I can just return the string
    //This returns null if no warning is needed
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

    //Extracting to a separate method to make the code easier on me!
    //This returns null if no warning is needed
    private string? GetImportantItemWarningLocMessage(List<CryoSleepWarningMessage.NetworkedWarningItem> warningItemsList,
        InventorySlotsComponent slotsComp)
    {
        if (warningItemsList.Count == 0)
            return null;
        //At this point in the code, none of these values should be null. If it is, something went *very* wrong in the code about
        var item1 = warningItemsList[0];
        var storageName1 = GetStorageName(item1, slotsComp);
        if (warningItemsList.Count == 1)
        {
            return Loc.GetString("accept-cryo-window-prompt-one-item-warning",
                ("item", Identity.Name(_entityManager.GetEntity(item1.Item), _entityManager)),
                ("storage", storageName1));
        }

        //We know there are at least 2 items now
        var item2 = warningItemsList[1];
        var key = warningItemsList.Count > 2 ? "accept-cryo-window-prompt-many-items-warning" : "accept-cryo-window-prompt-two-items-warning";
        var storageName2 = GetStorageName(item2, slotsComp);
        return Loc.GetString(key,
            ("item1", Identity.Name(_entityManager.GetEntity(item1.Item), _entityManager)),
            ("storage1", storageName1),
            ("item2", Identity.Name(_entityManager.GetEntity(item2.Item), _entityManager)),
            ("storage2", storageName2),
            //This key is not always needed, but put here to save a lot of copy pasting, and doesn't break the code, so :P
            ("num-extra-items", warningItemsList.Count - 2));

    }

    //Returns null if no warning is needed
    private string? GetUplinkWarningLocMessage(CryoSleepWarningMessage.NetworkedWarningItem foundUplink,
        InventorySlotsComponent slotsComp)
    {
        var localUplink = _entityManager.GetEntity(foundUplink.Item);
        if (!_entityManager.TryGetComponent<StoreComponent>(localUplink, out var store))
            return null;
        var currencyPrototype = store.Balance.Keys.First();
        var amount = store.Balance[currencyPrototype];
        if (amount == 0)
            return null;
        return Loc.GetString("accept-cryo-window-prompt-uplink-warning",
            ("uplink", Identity.Name(_entityManager.GetEntity(foundUplink.Item), _entityManager)),
            ("storage", GetStorageName(foundUplink, slotsComp)),
            ("amount", amount),
            //TODO: Properly localize the currency name
            ("currency", currencyPrototype));
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

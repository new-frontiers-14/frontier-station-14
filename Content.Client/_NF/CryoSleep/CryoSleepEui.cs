using System.Linq;
using Content.Client.Eui;
using Content.Client.Inventory;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.CryoSleep;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Components;
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

    public CryoSleepEui()
    {
        var playerEntity = IoCManager.Resolve<ISharedPlayerManager>().LocalEntity;

        _window = new AcceptCryoWindow();

        // Try to get the player's mind.
        if (!_entityManager.TryGetComponent(playerEntity, out JobTrackingComponent? jobTracking)
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


        //TODO: Find a good balance between checking everything and performance
        //TODO: Replace this with a full inventory scan, maybe write a helper method in another class?
        string[] slotsToCheck = ["back", "wallet"];
        var warningColor = Color.Yellow;

        //Scan the player's inventory slots
        //TODO: Refactor this with InventorySystem.TryGetSlotEntity
        //I spent so much time on this I am NOT rewriting this now
        if (_entityManager.TryGetComponent<InventorySlotsComponent>(playerEntity, out var slots))
        {
            var warningItemsList = new List<StorageHelper.FoundItem>();
            foreach (string slot in slotsToCheck)
            {
                var item = slots.SlotData[slot].HeldEntity;
                if (!item.HasValue)
                    continue;
                if (_entityManager.TryGetComponent<StorageComponent>(item, out var storageComp))
                    StorageHelper.ScanStorageForCondition(item.Value, ShouldItemWarnOnCryo, ref warningItemsList);
            }

            StorageHelper.FoundItem? foundShuttleDeed = null;
            var foundMoreShuttles = false;
            StorageHelper.FoundItem? foundUplink = null;
            //Shuttle and uplink check
            //Reverse for loop to allow for modification of the list while looping through it
            for (var i = warningItemsList.Count - 1; i >= 0 ; i--)
            {
                var foundItem = warningItemsList[i];
                if (_entityManager.HasComponent<ShuttleDeedComponent>(foundItem.Item))
                {
                    if (foundShuttleDeed.HasValue)
                        foundMoreShuttles = true;
                    else
                        foundShuttleDeed = foundItem;
                    //The item will be handled by a separate message, it shouldn't get lumped in with the other items
                    warningItemsList.RemoveAt(i);
                }
                else if (_entityManager.HasComponent<StoreComponent>(foundItem.Item))
                {
                    foundUplink = foundItem;
                    //See above
                    warningItemsList.RemoveAt(i);
                }
            }
            //At this point, the only items left in this list are things flagged with "WarnOnCryoSleep"

            //Check their ID card for any shuttle deeds, and update the warning as needed
            //The message chosser is in a different method to make the code easier to read
            //This is broken for the moment :(
            //var idCard = GetIDCardFromPDASlot(playerEntity.Value, _entityManager);
            var hasShuttleOnPDA = true;
                //(idCard.HasValue && _entityManager.HasComponent<ShuttleDeedComponent>(idCard));
            string? localizedShuttleWarning = GetShuttleWarningLocMessage(hasShuttleOnPDA, foundMoreShuttles, foundShuttleDeed, _entityManager);
            if (localizedShuttleWarning != null)
                _window.ShuttleWarningText.SetMessage(localizedShuttleWarning, null, warningColor);
            string? localizedItemWarningMessage = GetImportantItemWarningLocMessage(warningItemsList, _entityManager);
            if (localizedItemWarningMessage != null)
                _window.ItemWarningText.SetMessage(localizedItemWarningMessage, null, warningColor);
            if (foundUplink.HasValue)
            {
                string? localizedUplinkWarningMessage = GetUplinkWarningLocMessage(foundUplink.Value, _entityManager);
                if (localizedUplinkWarningMessage != null)
                    _window.UplinkWarningText.SetMessage(localizedUplinkWarningMessage, null, warningColor);
            }
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

    private bool ShouldItemWarnOnCryo(EntityUid ent)
    {
        return _entityManager.HasComponent<ShuttleDeedComponent>(ent)
               || _entityManager.HasComponent<WarnOnCryoSleepComponent>(ent)
               || _entityManager.HasComponent<StoreComponent>(ent);
    }

    //The if statement was too hard to read, so I moved it to its own method where I can just return the string
    //This returns null if no warning is needed
    private string? GetShuttleWarningLocMessage(bool hasShuttleOnPDA,
        bool foundMoreShuttles,
        StorageHelper.FoundItem? foundShuttleDeed,
        IEntityManager entityManager)
    {
        if (hasShuttleOnPDA)
        {
            if (!foundShuttleDeed.HasValue)
                return Loc.GetString("accept-cryo-window-prompt-shuttle-pda-warning");

            var key = foundMoreShuttles
                ? "accept cryo-window-prompt-shuttle-pda-and-many-bag-warning"
                : "accept-cryo-window-prompt-shuttle-pda-and-bag-warning";
            return Loc.GetString(key,
                ("deed", Identity.Name(foundShuttleDeed.Value.Item, entityManager)),
                ("storage", Identity.Name(foundShuttleDeed.Value.Container, entityManager)));
        }
        //They don't have a shuttle on their PDA, but you still gotta warn if its in the bag
        if (foundShuttleDeed.HasValue)
        {
            var key = foundMoreShuttles
                ? "accept cryo-window-prompt-shuttle-many-bag-warning"
                : "accept-cryo-window-prompt-shuttle-bag-warning";
            return Loc.GetString(key,
                ("deed", Identity.Name(foundShuttleDeed.Value.Item, entityManager)),
                ("storage", Identity.Name(foundShuttleDeed.Value.Container, entityManager)));

        }
        //No warning needed!
        return null;
    }

    //Extracting to a separate method to make the code easier on me!
    //This returns null if no warning is needed
    private string? GetImportantItemWarningLocMessage(List<StorageHelper.FoundItem> warningItemsList, IEntityManager manager)
    {
        if (warningItemsList.Count == 0)
            return null;
        //At this point in the code, none of these values should be null. If it is, something went *very* wrong in the code about
        var item1 = warningItemsList[0];
        if (warningItemsList.Count == 1)
            return Loc.GetString("accept-cryo-window-prompt-one-item-warning",
                ("item", item1.Item),
                ("storage", item1.Container));
        //We know there are at least 2 items now
        var item2 = warningItemsList[1];
        var key = warningItemsList.Count > 2 ? "accept-cryo-window-prompt-many-items-warning" : "accept-cryo-window-prompt-two-items-warning";
        return Loc.GetString(key,
            ("item1", Identity.Name(item1.Item, manager)),
            ("storage1", Identity.Name(item1.Container, manager)),
            ("item2", Identity.Name(item2.Item, manager)),
            ("storage2", Identity.Name(item2.Container, manager)),
            //This key is not always needed, but put here to save a lot of copy pasting, and doesn't break the code, so :P
            ("num-extra-items", warningItemsList.Count - 2));

    }

    //Returns null if no warning is needed
    private string? GetUplinkWarningLocMessage(StorageHelper.FoundItem foundUplink, IEntityManager manager)
    {
        if (manager.TryGetComponent<StoreComponent>(foundUplink.Item, out var store))
        {
            var currencyPrototype = store.Balance.Keys.First();
            var amount = store.Balance[currencyPrototype];
            if (amount == 0)
                return "";
            return Loc.GetString("accept-cryo-window-prompt-uplink-warning",
                ("uplink", Identity.Name(foundUplink.Item, manager)),
                ("storage", Identity.Name(foundUplink.Container, manager)),
                ("amount", amount),
                //TODO: Properly localize the currency name
                ("currency", currencyPrototype));
        }

        return null;
    }

    //Broken :(
    /*
    //Turned this into a method to avoid headaches as well
    //TODO: Possibly move this to a more central place
    EntityUid? GetIDCardFromPDASlot(EntityUid entity, IEntityManager manager)
    {

        if (_inventorySystem.TryGetSlotEntity(entity, "id", out var thingInIdSlot))
        {
            if (manager.HasComponent<IdCardComponent>(thingInIdSlot))
                return entity;
            if (manager.TryGetComponent<PdaComponent>(thingInIdSlot, out var pdaComp)
                && pdaComp.IdSlot.HasItem)
                return pdaComp.IdSlot.Item;
        }

        return null;
    }
    */

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

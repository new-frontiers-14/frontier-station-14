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


        //TODO: Find a good balance between checking everything and performance
        //TODO: Replace this with a full inventory scan, maybe write a helper method in another class?
        string[] slotsToCheck = ["back", "wallet"];
        var warningColor = Color.Yellow;

        //Scan the player's inventory slots
        //TODO: Refactor this with InventorySystem.TryGetSlotEntity
        //I spent so much time on this I am NOT rewriting this now
        if (entityManager.TryGetComponent<InventorySlotsComponent>(playerEntity, out var slots))
        {
            var warningItemsList = new List<StorageHelper.FoundItem>();
            foreach (string slot in slotsToCheck)
            {
                var item = slots.SlotData[slot].HeldEntity;
                if (!item.HasValue)
                    continue;
                if (entityManager.TryGetComponent<StorageComponent>(item, out var storageComp))
                    StorageHelper.ScanStorageForCondition(item.Value, ShouldItemWarnOnCryo, ref warningItemsList);
            }

            StorageHelper.FoundItem? foundShuttleDeed = null;
            var foundMoreShuttles = false;
            StorageHelper.FoundItem? foundUplink = null;
            //Shuttle and uplink check
            //TODO: Refactor to use something else so I can mark objects for removal withouth concurrent modification (java iterator equivalent?)
            foreach (var foundItem in warningItemsList)
            {
                if (entityManager.HasComponent<ShuttleDeedComponent>(foundItem.Item))
                {
                    if (foundShuttleDeed.HasValue)
                        foundMoreShuttles = true;
                    else
                        foundShuttleDeed = foundItem;
                }
                else if (entityManager.HasComponent<StoreComponent>(foundItem.Item))
                {
                    foundUplink = foundItem;
                }
            }

            //Check their ID card for any shuttle deeds, and update the warning as needed
            //The message chosser is in a different method to make the code easier to read
            var idCard = GetIDCardFromPDASlot(playerEntity.Value, entityManager);
            var hasShuttleOnPDA = (idCard.HasValue
                                   && entityManager.HasComponent<ShuttleDeedComponent>(idCard));
            string? localizedShuttleWarning = GetShuttleLocMessage(hasShuttleOnPDA, foundMoreShuttles, foundShuttleDeed, entityManager);
            if (localizedShuttleWarning != null)
                _window.ShuttleWarningText.SetMessage(localizedShuttleWarning, null, warningColor);

            //Now we check for and change items
            //TODO: Refactor this to use a better localization key chooser (Missing replacement keys don't matter!)
            if (warningItemsList.Count == 1)
            {
                var foundItem = warningItemsList[0];
                _window.ItemWarningText.SetMessage(Loc.GetString("accept-cryo-window-prompt-one-item-warning",
                    ("item", Identity.Name(foundItem.Item, entityManager)),
                    ("storage", Identity.Name(foundItem.Container, entityManager))),
                    null,
                    warningColor);
            }
            else if (warningItemsList.Count >= 2)
            {
                string locKey;
                locKey = warningItemsList.Count == 2 ? "accept-cryo-window-prompt-two-items-warning" : "accept-cryo-window-prompt-many-items-warning";
                var foundItem = warningItemsList[0];
                var foundItem2 = warningItemsList[1];
                _window.ItemWarningText.SetMessage(Loc.GetString(locKey,
                    ("item1", Identity.Name(foundItem.Item, entityManager)),
                    ("storage1", Identity.Name(foundItem.Container, entityManager)),
                    ("item2", Identity.Name(foundItem2.Item, entityManager)),
                    ("storage2", Identity.Name(foundItem2.Container, entityManager)),
                    //This key is not always needed, but put here to save a lot of copy pasting, and doesn't break the code, so :P
                    ("num-extra-items", warningItemsList.Count - 2)));
            }
            //TODO: Uplink checking when I am not hungry as hell


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
        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (entityManager.HasComponent<ShuttleDeedComponent>(ent) || entityManager.HasComponent<WarnOnCryoSleepComponent>(ent))
            return true;
        else
            return false;
    }

    //The if statement was too hard to read, so I moved it to its own method where I can just return the string
    //This returns an emtpy string if no warning is needed
    private string? GetShuttleLocMessage(bool hasShuttleOnPDA,
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

    //Turned this into a method to avoid headaches as well
    //TODO: Possibly move this to a more central place
    EntityUid? GetIDCardFromPDASlot(EntityUid entity, IEntityManager manager)
    {
        var inventorySystem = IoCManager.Resolve<InventorySystem>();
        if (inventorySystem.TryGetSlotEntity(entity, "id", out var thingInIdSlot))
        {
            if (manager.HasComponent<IdCardComponent>(thingInIdSlot))
                return entity;
            if (manager.TryGetComponent<PdaComponent>(thingInIdSlot, out var pdaComp)
                && pdaComp.IdSlot.HasItem)
                return pdaComp.IdSlot.Item;
        }

        return null;
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

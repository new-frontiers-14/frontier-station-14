using Content.Client.Eui;
using Content.Client.Inventory;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.CryoSleep;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.PDA;
using Content.Shared.Storage;
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
        string[] slotsToCheck = ["back", "wallet"];
        var warningColor = Color.Yellow;

        //Scan the player's inventory slots
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
            //Separately check their PDA slot
            var pda = slots.SlotData["id"].HeldEntity;
            if (pda != null &&
                entityManager.TryGetComponent<PdaComponent>(pda, out var pdaComp))
            {
                var idCard = pdaComp.IdSlot.Item;
                if (idCard != null
                    && entityManager.HasComponent<ShuttleDeedComponent>(idCard))
                    _window.ShuttleWarningText.SetMessage(Loc.GetString("accept-cryo-window-prompt-shuttle-warning"), null, warningColor);
            }
            //TODO: Check their bag for ID's with shuttle deeds and add them as well
            //TODO: Ask other people if there is a better way of doing this
            if (warningItemsList.Count == 1)
            {
                var foundItem = warningItemsList[0];
                _window.ItemWarningText.SetMessage(Loc.GetString("accept-cryo-window-prompt-one-item-warning",
                    ("item", Identity.Name(foundItem.Item, entityManager)),
                    ("storage", Identity.Name(foundItem.Container, entityManager))));
            }
            else if (warningItemsList.Count >= 2)
            {
                string locKey;
                if (warningItemsList.Count == 2)
                    locKey = "accept-cryo-window-prompt-two-items-warning";
                else
                    locKey = "accept-cryo-window-prompt-many-items-warning";
                var foundItem = warningItemsList[0];
                var foundItem2 = warningItemsList[1];
                _window.ItemWarningText.SetMessage(Loc.GetString(locKey,
                    ("item1", Identity.Name(foundItem.Item, entityManager)),
                    ("storage1", Identity.Name(foundItem.Container, entityManager)),
                    ("item2", Identity.Name(foundItem2.Item, entityManager)),
                    ("storage2", Identity.Name(foundItem2.Container, entityManager)),
                    //This key is not always needed, but put here to save a lot of copy pasting
                    ("num-extra-items", warningItemsList.Count - 2)));
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
        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (entityManager.HasComponent<ShuttleDeedComponent>(ent) || entityManager.HasComponent<WarnOnCryoSleepComponent>(ent))
            return true;
        else
            return false;
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

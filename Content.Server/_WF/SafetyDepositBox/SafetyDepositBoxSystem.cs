using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server._NF.Bank;
using Content.Shared._NF.Bank.Components;
using Content.Shared._WF.SafetyDepositBox;
using Content.Shared._WF.SafetyDepositBox.BUI;
using Content.Shared._WF.SafetyDepositBox.Components;
using Content.Shared._WF.SafetyDepositBox.Events;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using System.Linq;
using Content.Shared.Stacks;
using Content.Shared.Access.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Text.Json;
using Timer = Robust.Shared.Timing.Timer;
using YamlDotNet.RepresentationModel;

namespace Content.Server._WF.SafetyDepositBox;

public sealed class SafetyDepositBoxSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SafetyDepositConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, BoundUIOpenedEvent>(OnUIOpen);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, SafetyDepositPurchaseMessage>(OnPurchase);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, SafetyDepositDepositMessage>(OnDeposit);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, SafetyDepositWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, SafetyDepositReclaimMessage>(OnReclaim);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<SafetyDepositConsoleComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
    }

    private void OnConsoleInit(EntityUid uid, SafetyDepositConsoleComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, SafetyDepositConsoleComponent.BoxSlotId, component.BoxSlot);
    }

    private void OnUIOpen(EntityUid uid, SafetyDepositConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        UpdateUI(uid, component, player);
    }

    private async void UpdateUI(EntityUid consoleUid, SafetyDepositConsoleComponent component, EntityUid player)
    {
        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        var userId = actor.PlayerSession.UserId;
        if (!_prefsManager.TryGetCachedPreferences(userId, out var prefs))
            return;

        var characterIndex = prefs.SelectedCharacterIndex;

        // Get all boxes owned by this character from database
        var ownedBoxes = await _dbManager.GetPlayerSafetyDepositBoxes(userId.UserId, characterIndex);

        var boxInfoList = new List<SafetyDepositBoxInfo>();
        foreach (var box in ownedBoxes)
        {
            // A box is considered deposited if:
            // - It has never been withdrawn (!LastWithdrawn.HasValue), OR
            // - It was withdrawn in the current round and still has items
            // A box is considered lost if it was withdrawn in a previous round and has no items
            bool isDeposited;
            if (!box.LastWithdrawn.HasValue)
            {
                // Never withdrawn, so it's deposited
                isDeposited = true;
            }
            else if (box.LastWithdrawnRoundId.HasValue && box.LastWithdrawnRoundId.Value != _gameTicker.RoundId)
            {
                // Withdrawn in a previous round - lost regardless of items
                isDeposited = false;
            }
            else
            {
                // Withdrawn in current round - deposited only if it has items
                isDeposited = box.Items.Count > 0;
            }

            boxInfoList.Add(new SafetyDepositBoxInfo(
                box.BoxId,
                box.OwnerName,
                isDeposited,
                box.Nickname,
                box.BoxSize,
                box.LastWithdrawn,
                box.LastWithdrawnRoundId
            ));
        }

        var boxInSlot = component.BoxSlot.Item;
        SafetyDepositBoxInfo? boxInSlotInfo = null;

        if (boxInSlot != null && TryComp<SafetyDepositBoxComponent>(boxInSlot, out var boxComp) && boxComp.BoxId.HasValue)
        {
            // Get label if it exists
            string? nickname = null;
            if (TryComp<LabelComponent>(boxInSlot.Value, out var labelComp))
            {
                nickname = labelComp.CurrentLabel;
            }

            boxInSlotInfo = new SafetyDepositBoxInfo(
                boxComp.BoxId.Value,
                boxComp.OwnerName ?? "Unknown",
                false,
                nickname,
                "Unknown",
                null,
                null
            );
        }

        var state = new SafetyDepositConsoleState(
            boxInfoList,
            0, // No cash display needed anymore
            boxInSlot != null,
            boxInSlotInfo,
            component.TrialBoxCost,
            component.SmallBoxCost,
            component.MediumBoxCost,
            component.LargeBoxCost,
            _gameTicker.RoundId
        );

        _uiSystem.SetUiState(consoleUid, SafetyDepositConsoleUiKey.Key, state);
    }

    private void OnPurchase(EntityUid uid, SafetyDepositConsoleComponent component, SafetyDepositPurchaseMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        // Determine cost and prototype based on box size
        int cost;
        string prototypeId;
        switch (args.BoxSize)
        {
            case SafetyDepositBoxSize.Trial:
                cost = component.TrialBoxCost;
                prototypeId = "SafetyDepositBoxTrial";
                break;
            case SafetyDepositBoxSize.Small:
                cost = component.SmallBoxCost;
                prototypeId = "SafetyDepositBoxSmall";
                break;
            case SafetyDepositBoxSize.Medium:
                cost = component.MediumBoxCost;
                prototypeId = "SafetyDepositBoxMedium";
                break;
            case SafetyDepositBoxSize.Large:
                cost = component.LargeBoxCost;
                prototypeId = "SafetyDepositBoxLarge";
                break;
            default:
                ConsolePopup(player, "Error: Invalid box size.");
                PlayDenySound(uid, component);
                return;
        }

        // Check bank account
        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(player, "Error: No bank account found.");
            PlayDenySound(uid, component);
            return;
        }

        if (bank.Balance < cost)
        {
            ConsolePopup(player, $"Insufficient funds. You need ${cost:N0}, but only have ${bank.Balance:N0}.");
            PlayDenySound(uid, component);
            return;
        }

        // Withdraw from bank
        if (!_bankSystem.TryBankWithdraw(player, cost))
        {
            ConsolePopup(player, "Transaction failed.");
            PlayDenySound(uid, component);
            return;
        }

        // Create the box in the database
        var userId = actor.PlayerSession.UserId;
        if (!_prefsManager.TryGetCachedPreferences(userId, out var prefs))
        {
            ConsolePopup(player, "Error: Could not load character data.");
            PlayDenySound(uid, component);
            return;
        }

        var characterIndex = prefs.SelectedCharacterIndex;
        var characterName = MetaData(player).EntityName;

        // Check if trying to purchase a trial box and already owns one
        if (args.BoxSize == SafetyDepositBoxSize.Trial)
        {
            CheckTrialBoxLimitAsync(uid, component, player, userId.UserId, characterIndex, characterName, prototypeId, cost);
            return;
        }

        PurchaseBoxAsync(uid, component, player, userId.UserId, characterIndex, characterName, prototypeId, cost);
    }

    private async void CheckTrialBoxLimitAsync(
        EntityUid consoleUid,
        SafetyDepositConsoleComponent component,
        EntityUid player,
        Guid userId,
        int characterIndex,
        string characterName,
        string prototypeId,
        int cost)
    {
        var ownedBoxes = await _dbManager.GetPlayerSafetyDepositBoxes(userId, characterIndex);
        var hasTrialBox = ownedBoxes.Any(b => b.BoxSize == "Trial");

        if (hasTrialBox)
        {
            ConsolePopup(player, "You already own a Trial Box. Only one Trial Box per character is allowed.");
            PlayDenySound(consoleUid, component);
            return;
        }

        PurchaseBoxAsync(consoleUid, component, player, userId, characterIndex, characterName, prototypeId, cost);
    }

    private async void PurchaseBoxAsync(
        EntityUid consoleUid,
        SafetyDepositConsoleComponent component,
        EntityUid player,
        Guid userId,
        int characterIndex,
        string characterName,
        string prototypeId,
        int cost)
    {
        // Determine box size from prototype
        string boxSize = prototypeId switch
        {
            "SafetyDepositBoxTrial" => "Trial",
            "SafetyDepositBoxSmall" => "Small",
            "SafetyDepositBoxMedium" => "Medium",
            "SafetyDepositBoxLarge" => "Large",
            _ => "Small"
        };

        // Create box in database
        var box = await _dbManager.PurchaseSafetyDepositBox(userId, characterIndex, characterName, boxSize);

        // Spawn the physical box
        var boxEntity = Spawn(prototypeId, Transform(player).Coordinates);
        var boxComp = EnsureComp<SafetyDepositBoxComponent>(boxEntity);
        boxComp.BoxId = box.BoxId;
        boxComp.OwnerId = userId;
        boxComp.CharacterIndex = characterIndex;
        boxComp.OwnerName = characterName;
        boxComp.BoxPrototypeId = prototypeId;
        Dirty(boxEntity, boxComp);

        // Try to put it in player's hands
        if (!_hands.TryPickupAnyHand(player, boxEntity))
        {
            _transform.SetLocalRotation(boxEntity, Angle.Zero);
        }

        // Mark the box as withdrawn so it shows "In World" in the UI
        await _dbManager.ClearSafetyDepositBoxItems(box.BoxId, _gameTicker.RoundId);

        ConsolePopup(player, $"Safety deposit box purchased! Box ID: {box.BoxId.ToString()[..8]}...");
        PlayConfirmSound(consoleUid, component);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):actor} purchased safety deposit box {box.BoxId} for {cost} credits");

        UpdateUI(consoleUid, component, player);
    }

    private void OnDeposit(EntityUid uid, SafetyDepositConsoleComponent component, SafetyDepositDepositMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        // Check if there's a box in the slot
        var boxEntity = component.BoxSlot.Item;
        if (boxEntity == null)
        {
            ConsolePopup(player, "Please insert a safety deposit box.");
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<SafetyDepositBoxComponent>(boxEntity.Value, out var boxComp) || !boxComp.BoxId.HasValue)
        {
            ConsolePopup(player, "Invalid safety deposit box.");
            PlayDenySound(uid, component);
            return;
        }

        // Verify ownership
        var userId = actor.PlayerSession.UserId;
        if (!_prefsManager.TryGetCachedPreferences(userId, out var prefs))
        {
            ConsolePopup(player, "Error: Could not load character data.");
            PlayDenySound(uid, component);
            return;
        }

        var characterIndex = prefs.SelectedCharacterIndex;
        if (boxComp.OwnerId != userId.UserId || boxComp.CharacterIndex != characterIndex)
        {
            ConsolePopup(player, "This box does not belong to you.");
            PlayDenySound(uid, component);
            return;
        }

        // Serialize the contents
        if (!TryComp<StorageComponent>(boxEntity.Value, out var storageComp))
        {
            ConsolePopup(player, "Error: Box has no storage.");
            PlayDenySound(uid, component);
            return;
        }

        DepositBoxAsync(uid, component, player, boxEntity.Value, boxComp, storageComp);
    }

    private async void DepositBoxAsync(
        EntityUid consoleUid,
        SafetyDepositConsoleComponent component,
        EntityUid player,
        EntityUid boxEntity,
        SafetyDepositBoxComponent boxComp,
        StorageComponent storageComp)
    {
        _appearance.SetData(consoleUid, SafetyDepositConsoleVisuals.Printing, true);

        var entityDataList = new List<string>();

        Log.Info($"DepositBoxAsync: Box has {storageComp.Container.ContainedEntities.Count} items");

        // Serialize each item in the box - store prototype + component data
        foreach (var item in storageComp.Container.ContainedEntities)
        {
            try
            {
                Log.Info($"Serializing item: {ToPrettyString(item)}");

                // Blacklist ID cards - they should not be stored
                if (HasComp<IdCardComponent>(item))
                {
                    Log.Warning($"Item {ToPrettyString(item)} is an ID card, skipping");
                    continue;
                }

                // Get the prototype and metadata
                var prototype = MetaData(item).EntityPrototype;
                if (prototype == null)
                {
                    Log.Warning($"Item {ToPrettyString(item)} has no prototype, skipping");
                    continue;
                }

                var protoId = prototype.ID;

                // Create a JSON object to store entity data
                var entityData = new Dictionary<string, object>
                {
                    ["prototype"] = protoId
                };

                // Store paper content and stamps/signatures if it's a paper
                if (TryComp<PaperComponent>(item, out var paper))
                {
                    entityData["paperContent"] = paper.Content;

                    // Store stamps and signatures - store each stamp as a separate entry to preserve structure
                    if (paper.StampedBy.Count > 0)
                    {
                        // Store as a list that can be properly serialized
                        var stampsList = new List<Dictionary<string, object>>();
                        foreach (var stamp in paper.StampedBy)
                        {
                            stampsList.Add(new Dictionary<string, object>
                            {
                                ["stampedName"] = stamp.StampedName,
                                ["stampedColor"] = stamp.StampedColor.ToHex(),
                                ["stampType"] = (int)stamp.Type,
                                ["reapply"] = stamp.Reapply
                            });
                        }
                        entityData["paperStamps"] = stampsList;
                    }

                    if (!string.IsNullOrEmpty(paper.StampState))
                    {
                        entityData["paperStampState"] = paper.StampState;
                    }

                    Log.Info($"Stored paper content: {paper.Content.Substring(0, Math.Min(50, paper.Content.Length))}... with {paper.StampedBy.Count} stamps");
                }

                // Store label if it has one
                if (TryComp<LabelComponent>(item, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                {
                    entityData["label"] = label.CurrentLabel;
                    Log.Info($"Stored label: {label.CurrentLabel}");
                }

                // Store entity name if it differs from prototype default
                if (TryComp<MetaDataComponent>(item, out var metadata))
                {
                    var entityName = metadata.EntityName;
                    var prototypeName = metadata.EntityPrototype?.Name ?? "";

                    // Only store if custom name differs from prototype
                    if (!string.IsNullOrEmpty(entityName) && entityName != prototypeName)
                    {
                        entityData["entityName"] = entityName;
                        Log.Info($"Stored custom entity name: {entityName}");
                    }

                    // Store entity description if it differs from prototype default
                    var entityDesc = metadata.EntityDescription;
                    var prototypeDesc = metadata.EntityPrototype?.Description ?? "";

                    // Only store if custom description differs from prototype
                    if (!string.IsNullOrEmpty(entityDesc) && entityDesc != prototypeDesc)
                    {
                        entityData["entityDescription"] = entityDesc;
                        Log.Info($"Stored custom entity description: {entityDesc}");
                    }
                }

                // Store stack count if it's a stack
                if (TryComp<StackComponent>(item, out var stack))
                {
                    entityData["stackCount"] = stack.Count;
                    Log.Info($"Stored stack count: {stack.Count}");
                }

                // Serialize to JSON
                var json = JsonSerializer.Serialize(entityData);

                Log.Info($"Serialized as JSON: {json}");
                entityDataList.Add(json);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to serialize item {ToPrettyString(item)} in safety deposit box: {ex}");
            }
        }

        Log.Info($"Saving {entityDataList.Count} items to database for box {boxComp.BoxId}");

        // Get nickname from label if it exists
        string? nickname = null;
        if (TryComp<LabelComponent>(boxEntity, out var boxLabel) && !string.IsNullOrEmpty(boxLabel.CurrentLabel))
        {
            nickname = boxLabel.CurrentLabel;
            Log.Info($"Saving box nickname: {nickname}");
        }

        // Save to database
        await _dbManager.DepositSafetyDepositBoxItems(boxComp.BoxId!.Value, entityDataList);

        // Update nickname if one was set
        if (nickname != null)
        {
            await _dbManager.UpdateSafetyDepositBoxNickname(boxComp.BoxId!.Value, nickname);
        }

        // Remove from slot before deleting to properly update UI
        _itemSlots.TryEject(consoleUid, component.BoxSlot, null, out _);

        // Delete the physical box
        QueueDel(boxEntity);

        ConsolePopup(player, "Safety deposit box contents saved. The box has been stored.");
        PlayConfirmSound(consoleUid, component);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):actor} deposited safety deposit box {boxComp.BoxId} with {entityDataList.Count} items");

        Timer.Spawn(800, () =>
        {
            if (!Deleted(consoleUid))
            {
                _appearance.SetData(consoleUid, SafetyDepositConsoleVisuals.Printing, false);
            }
        });

        UpdateUI(consoleUid, component, player);
    }

    private void OnWithdraw(EntityUid uid, SafetyDepositConsoleComponent component, SafetyDepositWithdrawMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        var userId = actor.PlayerSession.UserId;
        if (!_prefsManager.TryGetCachedPreferences(userId, out var prefs))
            return;

        var characterIndex = prefs.SelectedCharacterIndex;

        WithdrawBoxAsync(uid, component, player, userId.UserId, characterIndex, args.BoxId);
    }

    private void OnReclaim(EntityUid uid, SafetyDepositConsoleComponent component, SafetyDepositReclaimMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        var userId = actor.PlayerSession.UserId;
        if (!_prefsManager.TryGetCachedPreferences(userId, out var prefs))
            return;

        var characterIndex = prefs.SelectedCharacterIndex;

        ReclaimBoxAsync(uid, component, player, userId.UserId, characterIndex, args.BoxId);
    }

    private async void ReclaimBoxAsync(
        EntityUid consoleUid,
        SafetyDepositConsoleComponent component,
        EntityUid player,
        Guid userId,
        int characterIndex,
        Guid boxId)
    {
        // Get box from database
        var box = await _dbManager.GetSafetyDepositBox(boxId);

        if (box == null)
        {
            ConsolePopup(player, "Box not found.");
            PlayDenySound(consoleUid, component);
            return;
        }

        // Verify ownership
        if (box.OwnerUserId != userId || box.CharacterIndex != characterIndex)
        {
            ConsolePopup(player, "This box does not belong to you.");
            PlayDenySound(consoleUid, component);
            return;
        }

        // Verify box is actually lost (withdrawn in previous round with no items)
        bool isLost = box.LastWithdrawn.HasValue &&
                      box.LastWithdrawnRoundId.HasValue &&
                      box.LastWithdrawnRoundId.Value != _gameTicker.RoundId &&
                      box.Items.Count == 0;

        if (!isLost)
        {
            ConsolePopup(player, "This box is not lost and cannot be reclaimed.");
            PlayDenySound(consoleUid, component);
            return;
        }

        // Delete the database record
        await _dbManager.DeleteSafetyDepositBox(boxId);

        // Create a new database record for the replacement box
        var newBox = await _dbManager.PurchaseSafetyDepositBox(
            userId,
            characterIndex,
            MetaData(player).EntityName,
            box.BoxSize
        );

        // Spawn a new empty physical box
        string prototypeId = box.BoxSize switch
        {
            "Small" => "SafetyDepositBoxSmall",
            "Medium" => "SafetyDepositBoxMedium",
            "Large" => "SafetyDepositBoxLarge",
            _ => "SafetyDepositBoxSmall"
        };

        var boxEntity = Spawn(prototypeId, Transform(player).Coordinates);
        var boxComp = EnsureComp<SafetyDepositBoxComponent>(boxEntity);
        boxComp.BoxId = newBox.BoxId;
        boxComp.OwnerId = userId;
        boxComp.CharacterIndex = characterIndex;
        boxComp.BoxPrototypeId = prototypeId;
        boxComp.OwnerName = MetaData(player).EntityName;
        Dirty(boxEntity, boxComp);

        // Mark the box as withdrawn in the current round (since we're giving them a physical box)
        await _dbManager.ClearSafetyDepositBoxItems(newBox.BoxId, _gameTicker.RoundId);

        // Restore nickname if one was saved
        if (!string.IsNullOrEmpty(box.Nickname))
        {
            _label.Label(boxEntity, box.Nickname);
        }

        // Try to put it in player's hands
        if (!_hands.TryPickupAnyHand(player, boxEntity))
        {
            _transform.SetLocalRotation(boxEntity, Angle.Zero);
        }

        ConsolePopup(player, "Lost box reclaimed! A new empty box has been issued.");
        PlayConfirmSound(consoleUid, component);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):actor} reclaimed lost safety deposit box {boxId}");

        UpdateUI(consoleUid, component, player);
    }

    private async void WithdrawBoxAsync(
        EntityUid consoleUid,
        SafetyDepositConsoleComponent component,
        EntityUid player,
        Guid userId,
        int characterIndex,
        Guid boxId)
    {
        // Get box from database
        var box = await _dbManager.GetSafetyDepositBox(boxId);

        if (box == null)
        {
            ConsolePopup(player, "Box not found.");
            PlayDenySound(consoleUid, component);
            return;
        }

        Log.Info($"WithdrawBoxAsync: Retrieved box {boxId} with {box.Items.Count} items from database");

        // Verify ownership
        if (box.OwnerUserId != userId || box.CharacterIndex != characterIndex)
        {
            ConsolePopup(player, "This box does not belong to you.");
            PlayDenySound(consoleUid, component);
            return;
        }

        // Spawn the physical box (use stored box size to determine prototype)
        string prototypeId = box.BoxSize switch
        {
            "Trial" => "SafetyDepositBoxTrial",
            "Small" => "SafetyDepositBoxSmall",
            "Medium" => "SafetyDepositBoxMedium",
            "Large" => "SafetyDepositBoxLarge",
            _ => "SafetyDepositBoxSmall"
        };

        var boxEntity = Spawn(prototypeId, Transform(player).Coordinates);
        var boxComp = EnsureComp<SafetyDepositBoxComponent>(boxEntity);
        boxComp.BoxId = box.BoxId;
        boxComp.OwnerId = userId;
        boxComp.CharacterIndex = characterIndex;
        boxComp.BoxPrototypeId = prototypeId;
        // Use current character name instead of stored name in case they changed it
        boxComp.OwnerName = MetaData(player).EntityName;
        Dirty(boxEntity, boxComp);

        // Restore nickname if one was saved
        if (!string.IsNullOrEmpty(box.Nickname))
        {
            _label.Label(boxEntity, box.Nickname);
            Log.Info($"Restored box nickname: {box.Nickname}");
        }

        // Deserialize and spawn items into the box
        if (TryComp<StorageComponent>(boxEntity, out var storageComp))
        {
            Log.Info($"Restoring {box.Items.Count} items to box storage");
            foreach (var itemData in box.Items)
            {
                try
                {
                    Log.Info($"Deserializing item, JSON length: {itemData.EntityData.Length}");

                    // Parse the JSON data
                    var entityData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(itemData.EntityData);

                    if (entityData == null || !entityData.ContainsKey("prototype"))
                    {
                        Log.Warning($"Invalid entity data: {itemData.EntityData}");
                        continue;
                    }

                    var protoId = entityData["prototype"].GetString();
                    if (protoId == null)
                    {
                        Log.Warning($"Could not extract prototype ID from JSON");
                        continue;
                    }

                    Log.Info($"Spawning entity with prototype: {protoId}");

                    // Spawn the entity from prototype at box location
                    var itemEntity = Spawn(protoId, Transform(boxEntity).Coordinates);

                    Log.Info($"Spawned entity: {ToPrettyString(itemEntity)}");

                    // Restore paper content, stamps, and signatures if present
                    if (TryComp<PaperComponent>(itemEntity, out var paper))
                    {
                        if (entityData.ContainsKey("paperContent"))
                        {
                            var content = entityData["paperContent"].GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                paper.Content = content;
                                Log.Info($"Restored paper content: {content.Substring(0, Math.Min(50, content.Length))}...");
                            }
                        }

                        // Restore stamps and signatures
                        if (entityData.ContainsKey("paperStamps"))
                        {
                            try
                            {
                                var stampsArray = entityData["paperStamps"].EnumerateArray();
                                var stampsList = new List<StampDisplayInfo>();

                                foreach (var stampElement in stampsArray)
                                {
                                    var stampInfo = new StampDisplayInfo
                                    {
                                        StampedName = stampElement.GetProperty("stampedName").GetString() ?? "",
                                        StampedColor = Color.FromHex(stampElement.GetProperty("stampedColor").GetString() ?? "#FFFFFF"),
                                        Type = (StampType)stampElement.GetProperty("stampType").GetInt32(),
                                        Reapply = stampElement.GetProperty("reapply").GetBoolean()
                                    };
                                    stampsList.Add(stampInfo);
                                }

                                if (stampsList.Count > 0)
                                {
                                    paper.StampedBy = stampsList;
                                    Log.Info($"Restored {stampsList.Count} stamps/signatures");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to restore stamps: {ex}");
                            }
                        }

                        if (entityData.ContainsKey("paperStampState"))
                        {
                            var stampState = entityData["paperStampState"].GetString();
                            if (!string.IsNullOrEmpty(stampState))
                            {
                                paper.StampState = stampState;
                            }
                        }

                        Dirty(itemEntity, paper);
                    }

                    // Restore label if present
                    if (entityData.ContainsKey("label"))
                    {
                        var labelText = entityData["label"].GetString();
                        if (!string.IsNullOrEmpty(labelText))
                        {
                            _label.Label(itemEntity, labelText);
                            Log.Info($"Restored label: {labelText}");
                        }
                    }

                    // Restore entity name if present
                    if (entityData.ContainsKey("entityName"))
                    {
                        var entityName = entityData["entityName"].GetString();
                        if (!string.IsNullOrEmpty(entityName))
                        {
                            if (TryComp<MetaDataComponent>(itemEntity, out var itemMetadata))
                            {
                                _metaDataSystem.SetEntityName(itemEntity, entityName, itemMetadata);
                                Log.Info($"Restored entity name: {entityName}");
                            }
                        }
                    }

                    // Restore entity description if present
                    if (entityData.ContainsKey("entityDescription"))
                    {
                        var entityDescription = entityData["entityDescription"].GetString();
                        if (!string.IsNullOrEmpty(entityDescription))
                        {
                            if (TryComp<MetaDataComponent>(itemEntity, out var itemMetadata))
                            {
                                _metaDataSystem.SetEntityDescription(itemEntity, entityDescription, itemMetadata);
                                Log.Info($"Restored entity description: {entityDescription}");
                            }
                        }
                    }

                    // Restore stack count if present
                    if (entityData.ContainsKey("stackCount") && TryComp<StackComponent>(itemEntity, out var stack))
                    {
                        var stackCount = entityData["stackCount"].GetInt32();
                        if (stackCount > 0)
                        {
                            stack.Count = stackCount;
                            Dirty(itemEntity, stack);
                            Log.Info($"Restored stack count: {stackCount}");
                        }
                    }

                    // Mark item as having been stored in a deposit box
                    EnsureComp<SafetyDepositStoredComponent>(itemEntity);

                    // Insert into storage
                    if (!_storage.Insert(boxEntity, itemEntity, out _, storageComp: storageComp, playSound: false))
                    {
                        Log.Warning($"Failed to insert {ToPrettyString(itemEntity)} into box storage, deleting");
                        QueueDel(itemEntity);
                    }
                    else
                    {
                        Log.Info($"Successfully inserted {ToPrettyString(itemEntity)} into box");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to deserialize item from safety deposit box {boxId}: {ex}");
                }
            }
        }
        else
        {
            Log.Error($"Box entity {boxEntity} has no StorageComponent!");
        }

        // Clear items from database
        await _dbManager.ClearSafetyDepositBoxItems(boxId, _gameTicker.RoundId);

        // Try to put it in player's hands or place it near them
        if (!_hands.TryPickupAnyHand(player, boxEntity))
        {
            _transform.SetLocalRotation(boxEntity, Angle.Zero);
        }

        ConsolePopup(player, "Safety deposit box retrieved.");
        PlayConfirmSound(consoleUid, component);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):actor} withdrew safety deposit box {boxId} with {box.Items.Count} items");

        UpdateUI(consoleUid, component, player);
    }

    private void OnSlotChanged(EntityUid uid, SafetyDepositConsoleComponent component, ContainerModifiedMessage args)
    {
        // Update UI for anyone who has this console's UI open
        foreach (var actor in _uiSystem.GetActors(uid, SafetyDepositConsoleUiKey.Key))
        {
            UpdateUI(uid, component, actor);
        }
    }

    private void PlayDenySound(EntityUid uid, SafetyDepositConsoleComponent component)
    {
        _audio.PlayPvs(component.ErrorSound, uid);
    }

    private void PlayConfirmSound(EntityUid uid, SafetyDepositConsoleComponent component)
    {
        _audio.PlayPvs(component.ConfirmSound, uid);
    }

    private void ConsolePopup(EntityUid actor, string text)
    {
        _popup.PopupEntity(text, actor, actor);
    }
}

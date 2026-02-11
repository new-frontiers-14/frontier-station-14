using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._NF.Paper;

public sealed class StaplerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaplerComponent, ActivateInWorldEvent>(OnStaplerActivate);
        SubscribeLocalEvent<StaplerComponent, ExaminedEvent>(OnStaplerExamined);
    }

    /// <summary>
    /// Attempts to use a stapler on a paper entity.
    /// If the stapler is empty, the paper is loaded into the stapler.
    /// If the stapler has a paper loaded, both papers are stapled into a new PaperBundle.
    /// Called by PaperSystem when a stapler is used on paper.
    /// </summary>
    /// <returns>True if the interaction was handled.</returns>
    public bool TryStaplePaper(EntityUid target, EntityUid used, EntityUid user)
    {
        if (!TryComp<StaplerComponent>(used, out var stapler))
            return false;

        if (!_itemSlots.TryGetSlot(used, stapler.SlotId, out var slot))
            return false;

        if (TryComp<UseDelayComponent>(used, out var useDelay) && _useDelay.IsDelayed((used, useDelay)))
            return false;

        if (slot.Item == null)
        {
            // Stapler is empty -- load the target paper into the stapler
            if (!_itemSlots.TryInsert(used, slot, target, user))
                return true; // Handled even if insert fails

            _popup.PopupClient(Loc.GetString("stapler-loaded-paper"), used, user);
            _audio.PlayPredicted(stapler.StapleSound, used, user);

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user):player} loaded {ToPrettyString(target):paper} into {ToPrettyString(used):stapler}");
        }
        else
        {
            // Stapler has a paper loaded -- create a new bundle with both papers
            var loadedPaper = slot.Item.Value;

            if (!_itemSlots.TryEject(used, slot, null, out _, excludeUserAudio: true))
                return true; // Handled even if eject fails

            if (_net.IsServer)
            {
                var bundleUid = Spawn("PaperBundle", Transform(target).Coordinates);
                var bundleContainer = _container.EnsureContainer<Container>(bundleUid, "bundle_papers");
                _container.Insert(loadedPaper, bundleContainer);
                _container.Insert(target, bundleContainer);
            }

            _popup.PopupClient(Loc.GetString("stapler-staple-success"), target, user);
            _audio.PlayPredicted(stapler.StapleSound, target, user);

            if (TryComp<UseDelayComponent>(used, out var delay))
                _useDelay.TryResetDelay((used, delay));

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user):player} stapled {ToPrettyString(loadedPaper):paper} and {ToPrettyString(target):paper} together");
        }

        return true;
    }

    /// <summary>
    /// Attempts to staple a paper from the stapler onto an existing bundle.
    /// Called by PaperBundleSystem when a stapler is used on a bundle.
    /// </summary>
    public bool TryAddToBundle(Entity<PaperBundleComponent> target, EntityUid user, EntityUid used)
    {
        if (!TryComp<StaplerComponent>(used, out var stapler))
            return false;

        if (!_itemSlots.TryGetSlot(used, stapler.SlotId, out var slot))
            return false;

        if (slot.Item == null)
            return false;

        if (TryComp<UseDelayComponent>(used, out var useDelay) && _useDelay.IsDelayed((used, useDelay)))
            return false;

        var bundleContainer = _container.GetContainer(target, target.Comp.ContainerId);
        if (bundleContainer.ContainedEntities.Count >= target.Comp.MaxPages)
        {
            _popup.PopupClient(Loc.GetString("stapler-bundle-full"), target, user);
            return true; // Handled even though full
        }

        var loadedPaper = slot.Item.Value;

        if (!_itemSlots.TryEject(used, slot, null, out _, excludeUserAudio: true))
            return false;

        if (_net.IsServer)
        {
            _container.Insert(loadedPaper, bundleContainer);
        }

        _popup.PopupClient(Loc.GetString("stapler-add-to-bundle"), target, user);
        _audio.PlayPredicted(stapler.StapleSound, target, user);

        if (TryComp<UseDelayComponent>(used, out var delay))
            _useDelay.TryResetDelay((used, delay));

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):player} stapled {ToPrettyString(loadedPaper):paper} onto {ToPrettyString(target):bundle}");

        return true;
    }

    /// <summary>
    /// Activating a stapler ejects the loaded paper back to the user's hand.
    /// </summary>
    private void OnStaplerActivate(Entity<StaplerComponent> stapler, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!_itemSlots.TryGetSlot(stapler, stapler.Comp.SlotId, out var slot))
            return;

        if (slot.Item == null)
        {
            _popup.PopupClient(Loc.GetString("stapler-empty"), stapler, args.User);
            return;
        }

        args.Handled = true;
        _itemSlots.TryEjectToHands(stapler, slot, args.User);
    }

    /// <summary>
    /// Shows whether the stapler has a paper loaded when examined.
    /// </summary>
    private void OnStaplerExamined(Entity<StaplerComponent> stapler, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_itemSlots.TryGetSlot(stapler, stapler.Comp.SlotId, out var slot))
            return;

        using (args.PushGroup(nameof(StaplerComponent)))
        {
            args.PushMarkup(slot.Item != null
                ? Loc.GetString("stapler-has-paper")
                : Loc.GetString("stapler-empty"));
        }
    }
}

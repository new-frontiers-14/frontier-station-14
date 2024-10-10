using Content.Server.Cargo.Systems;
using Content.Shared.Audio;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Server.Cargo.Components;
using Content.Shared.Timing;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class AppraisalCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CargoSystem _bountySystem = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppraisalCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<AppraisalCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<AppraisalCartridgeComponent, CartridgeActivatedEvent>(OnCartridgeActivated);
        SubscribeLocalEvent<AppraisalCartridgeComponent, CartridgeDeactivatedEvent>(OnCartridgeDeactivated);
    }

    // Kinda jank, but easiest way to get the right-click Appraisal verb to also work.
    // I'd much rather pass the GetUtilityVerb event through to the AppraisalCartridgeSystem and have all of
    // the functionality in there, rather than adding a PriceGunComponent to the PDA itself, but getting
    // that passthrough to work is not a straightforward thing.

    // Because of this weird workaround, items appraised with the right-click utility verb don't get added
    // to the history in the UI. That'll be something to revisit someday if anyone notices and complains :P

    // Doing this on cartridge activation and deactivation rather than install and remove so that the price
    // gun functionality is only there when the program is active.
    private void OnCartridgeActivated(Entity<AppraisalCartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        EnsureComp<PriceGunComponent>(args.Loader);
        // PriceGunSystem methods exit early if a DelayComponent is not present
        EnsureComp<UseDelayComponent>(args.Loader);
    }

    private void OnCartridgeDeactivated(Entity<AppraisalCartridgeComponent> ent, ref CartridgeDeactivatedEvent args)
    {
        var parent = Transform(args.Loader).ParentUid;
        RemComp<PriceGunComponent>(parent);
        RemComp<UseDelayComponent>(parent);
    }

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the Appraisal program and someone clicks on something with it. <br/>
    /// <br/>
    /// Does the thing... TODO
    /// </summary>
    private void AfterInteract(EntityUid uid, AppraisalCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
            return;

        var target = args.InteractEvent.Target;
        var who = args.InteractEvent.User;
        double price = 0.00;

        // All of the pop up display stuff is being handled by the PriceGunComponent addded to the PDA,
        // all we're doing in here is getting the price and recording it to the PDA interface bit.
        price = _pricingSystem.GetPrice(target.Value);

        //Limit the amount of saved probe results to 9
        //This is hardcoded because the UI doesn't support a dynamic number of results
        if (component.AppraisedItems.Count >= component.MaxSavedItems)
            component.AppraisedItems.RemoveAt(0);

        var item = new AppraisedItem(
            Name(target.Value),
            price.ToString("0.00")
        );

        component.AppraisedItems.Add(item);
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, AppraisalCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, AppraisalCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new AppraisalUiState(component.AppraisedItems);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}

// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 Whatstone
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._NF.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using static Content.Shared._NF.Atmos.Components.GasDepositScannerComponent;

namespace Content.Server._NF.Atmos.EntitySystems;

/// <summary>
/// Logic for the gas deposit scanner.  Largely based off of the GasAnalyzerSystem.
/// </summary>
[UsedImplicitly]
public sealed class GasDepositScannerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    /// <summary>
    /// Minimum moles of a gas to be included in the list.
    /// </summary>
    private const float UIMinMoles = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasDepositScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GasDepositScannerComponent, GasDepositScannerDisableMessage>(OnDisabledMessage);
        SubscribeLocalEvent<GasDepositScannerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveGasDepositScannerComponent>();
        while (query.MoveNext(out var uid, out var scanner))
        {
            // Don't update every tick
            scanner.AccumulatedFrametime += frameTime;

            if (scanner.AccumulatedFrametime < scanner.UpdateInterval)
                continue;

            scanner.AccumulatedFrametime -= scanner.UpdateInterval;

            if (!UpdateScanner(uid))
                RemCompDeferred<ActiveGasDepositScannerComponent>(uid);
        }
    }

    /// <summary>
    /// Activates the scanner when used in the world, scanning the target entity if it exists.
    /// </summary>
    private void OnAfterInteract(Entity<GasDepositScannerComponent> entity, ref AfterInteractEvent args)
    {
        var target = args.Target;
        if (target != null && !_interactionSystem.InRangeUnobstructed((args.User, null), (target.Value, null)))
        {
            target = null; // if the target is out of reach, invalidate it
            args.Handled = true;
            return;
        }

        if (target != null)
            ActivateScanner(entity, args.User, target);

        args.Handled = true;
    }

    /// <summary>
    /// Handles scanner activation logic.
    /// </summary>
    private void ActivateScanner(Entity<GasDepositScannerComponent> entity, EntityUid user, EntityUid? target = null)
    {
        if (!_userInterface.TryOpenUi(entity.Owner, GasDepositScannerUiKey.Key, user))
            return;

        entity.Comp.Target = target;
        entity.Comp.User = user;
        entity.Comp.Enabled = true;
        Dirty(entity);
        _appearance.SetData(entity.Owner, GasDepositScannerVisuals.Enabled, entity.Comp.Enabled);
        EnsureComp<ActiveGasDepositScannerComponent>(entity.Owner);
        UpdateScanner(entity.Owner, entity.Comp);
    }

    /// <summary>
    /// Close the UI, turn the scanner off, and don't update when it's dropped
    /// </summary>
    private void OnDropped(Entity<GasDepositScannerComponent> entity, ref DroppedEvent args)
    {
        if (args.User is var userId && entity.Comp.Enabled)
            _popup.PopupEntity(Loc.GetString("gas-deposit-scanner-shutoff"), userId, userId);
        DisableScanner(entity, args.User);
    }

    /// <summary>
    /// Closes the UI, sets the icon to off, and removes it from the update list.
    /// </summary>
    private void DisableScanner(Entity<GasDepositScannerComponent> entity, EntityUid? user = null)
    {
        _userInterface.CloseUi(entity.Owner, GasDepositScannerUiKey.Key, user);

        entity.Comp.Enabled = false;
        Dirty(entity);
        _appearance.SetData(entity.Owner, GasDepositScannerVisuals.Enabled, entity.Comp.Enabled);
        RemCompDeferred<ActiveGasDepositScannerComponent>(entity.Owner);
    }

    /// <summary>
    /// Disables the analyzer when the user closes the UI
    /// </summary>
    private void OnDisabledMessage(Entity<GasDepositScannerComponent> entity, ref GasDepositScannerDisableMessage message)
    {
        DisableScanner(entity);
    }

    /// <summary>
    /// Fetches fresh data for the scanner. Should only be called when the user requests an update.
    /// </summary>
    private bool UpdateScanner(EntityUid uid, GasDepositScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // Check if the user has walked away from what they scanned.
        if (component.Target.HasValue)
        {
            if (!_interactionSystem.InRangeUnobstructed((component.User, null), (component.Target.Value, null)))
            {
                if (component.User is { } userId && component.Enabled)
                    _popup.PopupEntity(Loc.GetString("gas-deposit-scanner-object-out-of-range"), userId, userId);

                component.Target = null;
                DisableScanner((uid, component), component.User);
                return false;
            }
        }

        GasEntry[]? gasMixList = null;

        if (component.Target != null)
        {
            if (Deleted(component.Target))
            {
                component.Target = null;
                DisableScanner((uid, component), component.User);
                return false;
            }

            if (!TryComp<GasDepositComponent>(component.Target, out var gasDeposit))
            {
                component.Target = null;
                DisableScanner((uid, component), component.User);
                return false;
            }

            gasMixList = GenerateGasEntryArray(gasDeposit.Composition, gasDeposit.GasLeft); // Mono
        }

        // Don't bother sending a UI message with no content, and stop updating I guess?
        if (gasMixList == null || gasMixList.Length <= 0)
            return false;

        _userInterface.ServerSendUiMessage(uid, GasDepositScannerUiKey.Key,
            new GasDepositScannerUserMessage(gasMixList.ToArray(),
                GetNetEntity(component.Target) ?? NetEntity.Invalid));
        return true;
    }

    /// <summary>
    /// Generates a GasEntry array for a given GasMixture.
    /// </summary>
    private GasEntry[] GenerateGasEntryArray(GasMixture? mixture,
                                             float scale) // Mono
    {
        if (mixture == null)
            return [];

        var gases = new List<GasEntry>();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gas = _atmos.GetGas(i);

            if (mixture[i] <= UIMinMoles)
                continue;

            var gasName = Loc.GetString(gas.Name);
            ApproximateGasDepositSize depositSize;
            // Mono
            var gasCount = mixture[i] * scale;
            if (gasCount < 500.0)
                depositSize = ApproximateGasDepositSize.Trace;
            else if (gasCount < 3000.0)
                depositSize = ApproximateGasDepositSize.Small;
            else if (gasCount < 10000.0)
                depositSize = ApproximateGasDepositSize.Medium;
            else if (gasCount < 30000.0)
                depositSize = ApproximateGasDepositSize.Large;
            else
                depositSize = ApproximateGasDepositSize.Enormous;
            gases.Add(new GasEntry(gasName, depositSize));
        }

        var gasesOrdered = gases.OrderByDescending(gas => gas.Amount);

        return gasesOrdered.ToArray();
    }
}

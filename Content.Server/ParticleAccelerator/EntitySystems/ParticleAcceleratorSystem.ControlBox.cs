using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Singularity.Components;
using Robust.Server.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using System.Diagnostics;
using Content.Shared.CCVar;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem
{
    private void InitializeControlBoxSystem()
    {
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, PowerChangedEvent>(OnControlBoxPowerChange);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorSetEnableMessage>(OnUISetEnableMessage);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorSetPowerStateMessage>(OnUISetPowerMessage);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorRescanPartsMessage>(OnUIRescanMessage);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ParticleAcceleratorControlBoxComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (controller.Firing && curTime >= controller.NextFire)
                Fire(uid, curTime, controller);
        }
    }

    [Conditional("DEBUG")]
    private void EverythingIsWellToFire(ParticleAcceleratorControlBoxComponent controller)
    {
        DebugTools.Assert(controller.Powered);
        DebugTools.Assert(controller.SelectedStrength != ParticleAcceleratorPowerState.Standby);
        DebugTools.Assert(controller.Assembled);
        DebugTools.Assert(EntityManager.EntityExists(controller.PortEmitter));
        DebugTools.Assert(EntityManager.EntityExists(controller.ForeEmitter));
        DebugTools.Assert(EntityManager.EntityExists(controller.StarboardEmitter));
    }

    public void Fire(EntityUid uid, TimeSpan curTime, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.LastFire = curTime;
        comp.NextFire = curTime + comp.ChargeTime;

        EverythingIsWellToFire(comp);

        var strength = comp.SelectedStrength;
        FireEmitter(comp.PortEmitter!.Value, strength);
        FireEmitter(comp.ForeEmitter!.Value, strength);
        FireEmitter(comp.StarboardEmitter!.Value, strength);
    }

    public void SwitchOn(EntityUid uid, IPlayerSession? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        DebugTools.Assert(comp.Assembled);

        if (comp.Enabled || !comp.CanBeEnabled)
            return;

        if (user?.AttachedEntity is { } player)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} has turned {ToPrettyString(uid)} on");

        comp.Enabled = true;
        UpdatePowerDraw(uid, comp);

        if (!TryComp<PowerConsumerComponent>(comp.PowerBox, out var powerConsumer)
        || powerConsumer.ReceivedPower >= powerConsumer.DrawRate * ParticleAcceleratorControlBoxComponent.RequiredPowerRatio)
            PowerOn(uid, comp);

        UpdateUI(uid, comp);
    }

    public void SwitchOff(EntityUid uid, IPlayerSession? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.Enabled)
            return;

        if (user?.AttachedEntity is { } player)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} has turned {ToPrettyString(uid)} off");

        comp.Enabled = false;
        UpdatePowerDraw(uid, comp);
        PowerOff(uid, comp);
        UpdateUI(uid, comp);
    }

    public void PowerOn(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        DebugTools.Assert(comp.Enabled);
        DebugTools.Assert(comp.Assembled);

        if (comp.Powered)
            return;

        comp.Powered = true;
        UpdatePowerDraw(uid, comp);
        UpdateFiring(uid, comp);
        UpdatePartVisualStates(uid, comp);
        UpdateUI(uid, comp);
    }

    public void PowerOff(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.Powered)
            return;

        comp.Powered = false;
        UpdatePowerDraw(uid, comp);
        UpdateFiring(uid, comp);
        UpdatePartVisualStates(uid, comp);
        UpdateUI(uid, comp);
    }

    public void SetStrength(EntityUid uid, ParticleAcceleratorPowerState strength, IPlayerSession? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (comp.StrengthLocked)
            return;

        strength = (ParticleAcceleratorPowerState) MathHelper.Clamp(
            (int) strength,
            (int) ParticleAcceleratorPowerState.Standby,
            (int) comp.MaxStrength
        );

        if (strength == comp.SelectedStrength)
            return;

        if (user?.AttachedEntity is { } player)
        {
            var impact = strength switch
            {
                ParticleAcceleratorPowerState.Standby => LogImpact.Low,
                ParticleAcceleratorPowerState.Level0 => LogImpact.Medium,
                ParticleAcceleratorPowerState.Level1 => LogImpact.High,
                ParticleAcceleratorPowerState.Level2
                or ParticleAcceleratorPowerState.Level3
                or _ => LogImpact.Extreme,
            };

            _adminLogger.Add(LogType.Action, impact, $"{ToPrettyString(player):player} has set the strength of {ToPrettyString(uid)} to {strength}");


            var alertMinPowerState = (ParticleAcceleratorPowerState)_cfg.GetCVar(CCVars.AdminAlertParticleAcceleratorMinPowerState);
            if (strength >= alertMinPowerState)
            {
                var pos = Transform(uid);
                _chat.SendAdminAlert(player, Loc.GetString("particle-accelerator-admin-power-strength-warning",
                    ("machine", ToPrettyString(uid)),
                    ("powerState", strength),
                    ("coordinates", pos.Coordinates)));
            }
        }

        comp.SelectedStrength = strength;
        UpdateAppearance(uid, comp);
        UpdatePartVisualStates(uid, comp);

        if (comp.Enabled)
        {
            UpdatePowerDraw(uid, comp);
            UpdateFiring(uid, comp);
        }
    }

    private void UpdateFiring(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!comp.Powered || comp.SelectedStrength < ParticleAcceleratorPowerState.Level0)
        {
            comp.Firing = false;
            return;
        }

        EverythingIsWellToFire(comp);

        var curTime = _gameTiming.CurTime;
        comp.LastFire = curTime;
        comp.NextFire = curTime + comp.ChargeTime;
        comp.Firing = true;
    }

    private void UpdatePowerDraw(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!TryComp<PowerConsumerComponent>(comp.PowerBox, out var powerConsumer))
            return;

        var powerDraw = comp.BasePowerDraw;
        if (comp.Enabled)
            powerDraw += comp.LevelPowerDraw * (int) comp.SelectedStrength;

        powerConsumer.DrawRate = powerDraw;
    }

    private void UpdateUI(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!_uiSystem.TryGetUi(uid, ParticleAcceleratorControlBoxUiKey.Key, out var bui))
            return;

        var draw = 0f;
        var receive = 0f;

        if (TryComp<PowerConsumerComponent>(comp.PowerBox, out var powerConsumer))
        {
            draw = powerConsumer.DrawRate;
            receive = powerConsumer.ReceivedPower;
        }

        _uiSystem.SetUiState(bui, new ParticleAcceleratorUIState(
            comp.Assembled,
            comp.Enabled,
            comp.SelectedStrength,
            (int) draw,
            (int) receive,
            comp.StarboardEmitter != null,
            comp.ForeEmitter != null,
            comp.PortEmitter != null,
            comp.PowerBox != null,
            comp.FuelChamber != null,
            comp.EndCap != null,
            comp.InterfaceDisabled,
            comp.MaxStrength,
            comp.StrengthLocked
        ));
    }

    private void UpdateAppearance(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _appearanceSystem.SetData(
            uid,
            ParticleAcceleratorVisuals.VisualState,
            TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered
                ? ParticleAcceleratorVisualState.Unpowered
                : (ParticleAcceleratorVisualState) comp.SelectedStrength,
            appearance
        );
    }

    private void UpdatePartVisualStates(EntityUid uid, ParticleAcceleratorControlBoxComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        var state = controller.Powered ? (ParticleAcceleratorVisualState) controller.SelectedStrength : ParticleAcceleratorVisualState.Unpowered;

        // UpdatePartVisualState(ControlBox); (We are the control box)
        if (controller.FuelChamber.HasValue)
            _appearanceSystem.SetData(controller.FuelChamber!.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (controller.PowerBox.HasValue)
            _appearanceSystem.SetData(controller.PowerBox!.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (controller.PortEmitter.HasValue)
            _appearanceSystem.SetData(controller.PortEmitter!.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (controller.ForeEmitter.HasValue)
            _appearanceSystem.SetData(controller.ForeEmitter!.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (controller.StarboardEmitter.HasValue)
            _appearanceSystem.SetData(controller.StarboardEmitter!.Value, ParticleAcceleratorVisuals.VisualState, state);
        //no endcap because it has no powerlevel-sprites
    }

    private IEnumerable<EntityUid> AllParts(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (Resolve(uid, ref comp))
        {
            if (comp.FuelChamber.HasValue)
                yield return comp.FuelChamber.Value;
            if (comp.EndCap.HasValue)
                yield return comp.EndCap.Value;
            if (comp.PowerBox.HasValue)
                yield return comp.PowerBox.Value;
            if (comp.PortEmitter.HasValue)
                yield return comp.PortEmitter.Value;
            if (comp.ForeEmitter.HasValue)
                yield return comp.ForeEmitter.Value;
            if (comp.StarboardEmitter.HasValue)
                yield return comp.StarboardEmitter.Value;
        }
    }

    private void OnComponentStartup(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ComponentStartup args)
    {
        if (TryComp<ParticleAcceleratorPartComponent>(uid, out var part))
            part.Master = uid;
    }

    private void OnComponentShutdown(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ComponentShutdown args)
    {
        if (TryComp<ParticleAcceleratorPartComponent>(uid, out var partStatus))
            partStatus.Master = null;

        var partQuery = GetEntityQuery<ParticleAcceleratorPartComponent>();
        foreach (var part in AllParts(uid, comp))
        {
            if (partQuery.TryGetComponent(part, out var partData))
                partData.Master = null;
        }
    }

    // This is the power state for the PA control box itself.
    // Keep in mind that the PA itself can keep firing as long as the HV cable under the power box has... power.
    private void OnControlBoxPowerChange(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid, comp);

        if (!args.Powered)
            _uiSystem.TryCloseAll(uid, ParticleAcceleratorControlBoxUiKey.Key);
    }

    private void OnUISetEnableMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorSetEnableMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        if (msg.Enabled)
        {
            if (comp.Assembled)
                SwitchOn(uid, (IPlayerSession?) msg.Session, comp);
        }
        else
            SwitchOff(uid, (IPlayerSession?) msg.Session, comp);

        UpdateUI(uid, comp);
    }

    private void OnUISetPowerMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorSetPowerStateMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        SetStrength(uid, msg.State, (IPlayerSession?) msg.Session, comp);

        UpdateUI(uid, comp);
    }

    private void OnUIRescanMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorRescanPartsMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        RescanParts(uid, (IPlayerSession?) msg.Session, comp);

        UpdateUI(uid, comp);
    }
}

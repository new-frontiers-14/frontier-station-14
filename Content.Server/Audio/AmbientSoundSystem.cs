using Content.Server._NF.Audio; // Frontier
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Sound.Components; // Frontier
using Content.Shared.Audio;
using Content.Shared.Mobs; // Frontier

namespace Content.Server.Audio;

public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
        SubscribeLocalEvent<SoundWhileAliveComponent, MobStateChangedEvent>(HandleMobDeath);
    }

    private void HandleMobDeath(EntityUid uid, SoundWhileAliveComponent component, MobStateChangedEvent args) // Frontier
    {
        SetAmbience(uid, args.NewMobState != MobState.Dead);

        if (TryComp<SpamEmitSoundComponent>(uid, out var comp))
            comp.Enabled = args.NewMobState == MobState.Alive;
    }

    private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
    {
        SetAmbience(uid, args.Supply);
    }

    private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, ref PowerChangedEvent args)
    {
        SetAmbience(uid, args.Powered);
    }
}

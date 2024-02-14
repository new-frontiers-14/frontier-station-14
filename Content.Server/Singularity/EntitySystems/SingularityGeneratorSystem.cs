using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class SingularityGeneratorSystem : EntitySystem
{
    #region Dependencies
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);

        var vvHandle = _vvm.GetTypeHandler<SingularityGeneratorComponent>();
        vvHandle.AddPath(nameof(SingularityGeneratorComponent.Power), (_, comp) => comp.Power, SetPower);
        vvHandle.AddPath(nameof(SingularityGeneratorComponent.Threshold), (_, comp) => comp.Threshold, SetThreshold);
    }

    public override void Shutdown()
    {
        var vvHandle = _vvm.GetTypeHandler<SingularityGeneratorComponent>();
        vvHandle.RemovePath(nameof(SingularityGeneratorComponent.Power));
        vvHandle.RemovePath(nameof(SingularityGeneratorComponent.Threshold));

        base.Shutdown();
    }


    /// <summary>
    /// Handles what happens when a singularity generator passes its power threshold.
    /// Default behavior is to reset the singularities power level and spawn a singularity.
    /// </summary>
    /// <param name="uid">The uid of the singularity generator.</param>
    /// <param name="comp">The state of the singularity generator.</param>
    private void OnPassThreshold(EntityUid uid, SingularityGeneratorComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        SetPower(uid, 0, comp);
        EntityManager.SpawnEntity(comp.SpawnPrototype, Transform(uid).Coordinates);
    }

    #region Getters/Setters
    /// <summary>
    /// Setter for <see cref="SingularityGeneratorComponent.Power"/>
    /// If the singularity generator passes its threshold it also spawns a singularity.
    /// </summary>
    /// <param name="comp">The singularity generator component.</param>
    /// <param name="value">The new power level for the generator component to have.</param>
    public void SetPower(EntityUid uid, float value, SingularityGeneratorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var oldValue = comp.Power;
        if (value == oldValue)
            return;

        comp.Power = value;
        if (comp.Power >= comp.Threshold && oldValue < comp.Threshold)
            OnPassThreshold(uid, comp);
    }

    /// <summary>
    /// Setter for <see cref="SingularityGeneratorComponent.Threshold"/>
    /// If the singularity generator has passed its new threshold it also spawns a singularity.
    /// </summary>
    /// <param name="comp">The singularity generator component.</param>
    /// <param name="value">The new threshold power level for the generator component to have.</param>
    public void SetThreshold(EntityUid uid, float value, SingularityGeneratorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var oldValue = comp.Threshold;
        if (value == comp.Threshold)
            return;

        comp.Power = value;
        if (comp.Power >= comp.Threshold && comp.Power < oldValue)
            OnPassThreshold(uid, comp);
    }
    #endregion Getters/Setters

    #region Event Handlers
    /// <summary>
    /// Handles PA Particles colliding with a singularity generator.
    /// Adds the power from the particles to the generator.
    /// TODO: Desnowflake this.
    /// </summary>
    /// <param name="uid">The uid of the PA particles have collided with.</param>
    /// <param name="component">The state of the PA particles.</param>
    /// <param name="args">The state of the beginning of the collision.</param>
    private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, ref StartCollideEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityGeneratorComponent>(args.OtherEntity, out var singularityGeneratorComponent))
        {
            SetPower(
                args.OtherEntity,
                singularityGeneratorComponent.Power + component.State switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 2,
                    ParticleAcceleratorPowerState.Level2 => 4,
                    ParticleAcceleratorPowerState.Level3 => 8,
                    _ => 0
                },
                singularityGeneratorComponent
            );
            EntityManager.QueueDeleteEntity(uid);
        }
    }
    #endregion Event Handlers
}

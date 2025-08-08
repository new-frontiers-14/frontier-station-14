using Content.Shared._NF.Skrungler.Components;
using Content.Shared.Audio;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Jittering;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Skrungler;

/// <summary>
/// Lets you turn other mobs into plasma fuel.
/// <seealso cref="SkrunglerComponent"/>
/// </summary>
public abstract class SharedSkrunglerSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkrunglerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SkrunglerComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<SkrunglerComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
    }

    private void OnExamined(Entity<SkrunglerComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        using (args.PushGroup(nameof(SkrunglerComponent)))
        {
            if (_appearance.TryGetData<bool>(ent, SkrunglerVisuals.Skrungling, out var isSkrungling, appearance) &&
                isSkrungling)
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-is-running",
                    ("owner", ent)));
            }

            if (_appearance.TryGetData<bool>(ent, StorageVisuals.HasContents, out var hasContents, appearance) &&
                hasContents)
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-has-contents"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-empty"));
            }
        }
    }

    private void OnStorageOpenAttempt(Entity<SkrunglerComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (ent.Comp.Active)
            args.Cancelled = true;
    }

    private void OnUnanchorAttempt(Entity<SkrunglerComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.Active)
            args.Cancel();
    }

    protected virtual void StartProcessing(EntityUid uid, Entity<SkrunglerComponent> skrungler)
    {
        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        var curTime = Timing.CurTime;

        var expectedYield = physics.FixturesMass * skrungler.Comp.YieldPerUnitMass;
        skrungler.Comp.CurrentExpectedYield += expectedYield;

        skrungler.Comp.FinishProcessingTime = curTime + physics.FixturesMass * skrungler.Comp.ProcessingTimePerUnitMass;
        skrungler.Comp.Active = true;
        skrungler.Comp.NextMessTime = curTime + skrungler.Comp.MessInterval;

        Dirty(skrungler);
        StartProcessingVisuals(skrungler);
        QueueDel(uid);
    }

    private void StartProcessingVisuals(Entity<SkrunglerComponent> ent)
    {
        _appearance.SetData(ent, SkrunglerVisuals.SkrunglingBase, true);
        _appearance.SetData(ent, SkrunglerVisuals.Skrungling, true);
        _jittering.AddJitter(ent, -85, 0); // High frequency, low amplitude jitter.
        _audio.PlayPvs(ent.Comp.SkrungStartSound, ent);
        _audio.PlayPvs(ent.Comp.SkrunglerSound, ent);
        _ambientSound.SetAmbience(ent, true);
    }

    protected void EndProcessingVisuals(Entity<SkrunglerComponent> ent)
    {
        _appearance.SetData(ent, SkrunglerVisuals.SkrunglingBase, false);
        _appearance.SetData(ent, SkrunglerVisuals.Skrungling, false);
        RemCompDeferred<JitteringComponent>(ent);
        _audio.PlayPvs(ent.Comp.SkrungFinishSound, ent);
        _ambientSound.SetAmbience(ent, false);
    }
}

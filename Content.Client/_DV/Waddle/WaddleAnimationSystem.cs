using System.Numerics;
using Content.Shared._DV.Waddle;
using Content.Shared.Movement.Components;
using Content.Shared.Standing; //imp edit
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._DV.Waddle;

public sealed class WaddleAnimationSystem : SharedWaddleAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!; //imp edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleAnimationComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnHandleState(Entity<WaddleAnimationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAnimation(ent);
    }

    private void OnAnimationCompleted(Entity<WaddleAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != ent.Comp.KeyName || !ent.Comp.IsWaddling)
            return;

        PlayAnimation(ent);
    }

    protected override void UpdateAnimation(Entity<WaddleAnimationComponent> ent)
    {
        if (ent.Comp.IsWaddling)
            StartAnimation(ent);
        else
            StopAnimation(ent);
    }

    private void StartAnimation(Entity<WaddleAnimationComponent> ent)
    {
        if (_animation.HasRunningAnimation(ent, ent.Comp.KeyName))
            return;

        PlayAnimation(ent);
    }

    private void StopAnimation(Entity<WaddleAnimationComponent> ent)
    {
        _animation.Stop(ent.Owner, ent.Comp.KeyName);

        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        // Imp edit: exit the method if the entity is already down
        if (_standing.IsDown(ent))
            return;

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
    }

    private void PlayAnimation(Entity<WaddleAnimationComponent> ent)
    {
        PlayWaddleAnimationUsing(
            ent,
            CalculateAnimationLength(ent),
            CalculateTumbleIntensity(ent)
        );
    }

    private static float CalculateTumbleIntensity(WaddleAnimationComponent comp) //imp edit, made static
    {
        return comp.LastStep ? 360 - comp.TumbleIntensity : comp.TumbleIntensity;
    }

    private TimeSpan CalculateAnimationLength(Entity<WaddleAnimationComponent> ent)
    {
        var length = ent.Comp.AnimationLength;
        if (CompOrNull<InputMoverComponent>(ent)?.Sprinting == true)
            return length * ent.Comp.RunAnimationLengthMultiplier;

        return length;
    }

    private void PlayWaddleAnimationUsing(Entity<WaddleAnimationComponent> ent, TimeSpan len, float tumbleIntensity)
    {
        ent.Comp.LastStep = !ent.Comp.LastStep;

        // jump peaks halfway through the animation
        var peak = (float)len.TotalSeconds * 0.5f;
        var anim = new Animation()
        {
            Length = len,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(tumbleIntensity), peak),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), peak),
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(), 0),
                        new AnimationTrackProperty.KeyFrame(ent.Comp.HopIntensity, peak),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), peak),
                    }
                }
            }
        };

        _animation.Play(ent.Owner, anim, ent.Comp.KeyName);
    }
}

<<<<<<< HEAD
namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Base class for all throw events.
    /// </summary>
    public abstract class ThrowEvent : HandledEntityEventArgs
    {
        ///Nyano - Summary: Allows us to tell who threw the item. It matters!
        /// <summary>
        ///     The entity that threw <see cref="Thrown"/>.
        /// </summary>
        public EntityUid? User { get; }
        // End Nyano code. 
        public readonly EntityUid Thrown;
        public readonly EntityUid Target;
        public ThrownItemComponent Component;

        public ThrowEvent(EntityUid? user, EntityUid thrown, EntityUid target, ThrownItemComponent component) //Nyano - Summary: User added.
        {
            User = user; //Nyano - Summary: User added.
            Thrown = thrown;
            Target = target;
            Component = component;
        }
    }

    /// <summary>
    ///     Raised directed on the target entity being hit by the thrown entity.
    /// </summary>
    public sealed class ThrowHitByEvent : ThrowEvent
    {
        public ThrowHitByEvent(EntityUid? user, EntityUid thrown, EntityUid target, ThrownItemComponent component) : base(user, thrown, target, component) //Nyano - Summary: User added.
        {
        }
    }

    /// <summary>
    ///     Raised directed on the thrown entity that hits another.
    /// </summary>
    public sealed class ThrowDoHitEvent : ThrowEvent
    {
        public ThrowDoHitEvent(EntityUid thrown, EntityUid target, ThrownItemComponent component) : base(null, thrown, target, component) //Nyano - Summary: User added.
        {
        }
    }
}
=======
namespace Content.Shared.Throwing;

/// <summary>
/// Raised on an entity after it has thrown something.
/// </summary>
[ByRefEvent]
public readonly record struct ThrowEvent(EntityUid? User, EntityUid Thrown);

/// <summary>
/// Raised on an entity after it has been thrown.
/// </summary>
[ByRefEvent]
public readonly record struct ThrownEvent(EntityUid? User, EntityUid Thrown);

/// <summary>
/// Raised directed on the target entity being hit by the thrown entity.
/// </summary>
[ByRefEvent]
public readonly record struct ThrowHitByEvent(EntityUid Thrown, EntityUid Target, ThrownItemComponent Component);

/// <summary>
/// Raised directed on the thrown entity that hits another.
/// </summary>
[ByRefEvent]
public readonly record struct ThrowDoHitEvent(EntityUid Thrown, EntityUid Target, ThrownItemComponent Component);
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78

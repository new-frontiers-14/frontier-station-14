using Content.Server.Speech.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

/// <summary>
///     Applies accent to user while they hold the entity.
/// </summary>
[RegisterComponent]
public sealed partial class AddAccentPickupComponent : Component
{
    /// <summary>
    ///     Component name for accent that will be applied.
    /// </summary>
    [DataField("accent", required: true)]
    public string Accent = default!;

    /// <summary>
    ///     What <see cref="ReplacementAccentPrototype"/> to use.
    ///     Will be applied only with <see cref="ReplacementAccentComponent"/>.
    /// </summary>
    [DataField("replacement", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string? ReplacementPrototype;

    /// <summary>
    ///     Is the entity held and affecting someones accent?
    /// </summary>
    public bool IsActive = false;

    /// <summary>
    ///     Who is currently holding the item?
    /// </summary>
    public EntityUid Holder; // Frontier
}

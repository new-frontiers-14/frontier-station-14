using Content.Server._NF.RandomSpeak.EntitySystems;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.RandomSpeak.Components;

/// <summary>
/// Makes this entity periodically speak by speaking a randomly selected
/// message from a specified dataset into local chat.
/// </summary>
[RegisterComponent, Access(typeof(RandomSpeakSystem))]
public sealed partial class RandomSpeakComponent : Component
{
    /// <summary>
    /// Minimum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal to 1.
    /// </summary>
    [DataField]
    public int MinimumWait { get; private set; } = 8 * 60;

    /// <summary>
    /// Maximum time in seconds to wait before speaking, in seconds. Has to be larger than or equal
    /// to <see cref="MinimumWait"/>
    /// </summary>
    [DataField]
    public int MaximumWait { get; private set; } = 10 * 60;

    /// <summary>
    /// If true, the delay before the first speaking event (at MapInit) will ignore <see cref="MinimumWait"/>
    /// and instead be rolled between 0 and <see cref="MaximumWait"/>. This only applies to the initial delay;
    /// <see cref="MinimumWait"/> will be respected after that.
    /// </summary>
    [DataField]
    public bool Prewarm = true;

    /// <summary>
    /// The identifier for the sentences dataset prototype.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Pack { get; private set; }

    /// <summary>
    /// The next time sentence will be said.
    /// </summary>
    [DataField]
    public TimeSpan NextRandomSpeakTime { get; set; } = TimeSpan.Zero;

}

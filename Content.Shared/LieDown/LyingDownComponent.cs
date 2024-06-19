using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LieDown;

/// <summary>
///     Makes the target to lie down.
/// </summary>
[Access(typeof(SharedLieDownSystem))]
[RegisterComponent, NetworkedComponent()]
public sealed partial class LyingDownComponent : Component
{
    /// <summary>
    ///     The action to lie down or stand up.
    /// </summary>
    [DataField]
    public EntProtoId? MakeToStandUpAction = "action-name-make-standup";
}

[Serializable, NetSerializable]
public sealed class ChangeStandingStateEvent : EntityEventArgs {}

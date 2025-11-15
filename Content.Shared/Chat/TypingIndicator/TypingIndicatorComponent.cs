using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Show typing indicator icon when player typing text in chat box.
///     Added automatically when player poses entity.
/// </summary>
<<<<<<< HEAD
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // DeltaV - added AutoGenerateComponentState
=======
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Prototype id that store all visual info about typing indicator.
    /// </summary>
    [DataField("proto"), AutoNetworkedField]
    public ProtoId<TypingIndicatorPrototype> TypingIndicatorPrototype = "default";

    /// <summary>
    ///  DeltaV - Allow the indicator to be temporarily overriden
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<TypingIndicatorPrototype>? TypingIndicatorOverridePrototype;
}

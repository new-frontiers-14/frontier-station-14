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
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78
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

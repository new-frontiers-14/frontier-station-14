using Robust.Shared.Serialization;

namespace Content.Shared._CitadelStation.ERP.Interaction.UI.Messages;

[Serializable, NetSerializable]
public sealed class ERPInteractionMenuBoundUserInterfaceState : BoundUserInterfaceState {

}

[Serializable, NetSerializable]
public enum ERPInteractionMenuInterface
{
    Key
}

[Serializable, NetSerializable]
public enum ERPInteractionMenuInteractorOptions
{
    Mouth,
    Hand,
    Groin
}

[Serializable, NetSerializable]
public sealed class ERPInteractionMenuChooseInteractorOption(ERPInteractionMenuInteractorOptions bodyPart) : BoundUserInterfaceMessage
{
    public readonly ERPInteractionMenuInteractorOptions BodyPart = bodyPart;
}

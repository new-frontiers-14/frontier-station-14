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
public enum ERPInteractionMenuInteractorModes
{
    Gentle,
    Rough,
    Violent
}

[Serializable, NetSerializable]
public enum ERPInteractionMenuBodyParts
{
    Head,
    LArm,
    Chest,
    RArm,
    LForearm,
    Abdomen,
    RForearm,
    LHand,
    Groin,
    RHand
}

[Serializable, NetSerializable]
public sealed class ERPActionUserInterfaceMessage : BoundUserInterfaceMessage {
    public ERPInteractionMenuInteractorOptions Interactor;
    public ERPInteractionMenuInteractorModes Mode;
    public ERPInteractionMenuBodyParts Bodypart;
}

[Serializable, NetSerializable]
public sealed class ERPInteractionMenuChooseInteractorOption(ERPInteractionMenuInteractorOptions bodyPart) : BoundUserInterfaceMessage
{
    public readonly ERPInteractionMenuInteractorOptions BodyPart = bodyPart;
}

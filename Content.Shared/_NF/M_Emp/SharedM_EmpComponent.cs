using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.M_Emp;

[NetworkedComponent, RegisterComponent, Virtual]
public sealed class SharedM_EmpComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class M_EmpBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasPower;
    public M_EmpBoundUserInterfaceState(bool hasPower)
    {
        HasPower = hasPower;
    }
}

[Serializable, NetSerializable]
public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly UiButton Button;

    public UiButtonPressedMessage(UiButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public enum M_EmpUiKey
{
    Key
}

public enum UiButton
{
    Request,
    Activate,
}

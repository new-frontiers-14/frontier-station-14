using Robust.Shared.GameStates;

namespace Content.Server.Emp;

/// <summary>
/// Prevents EMP when attached to a grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmpImmuneGridComponent : Component
{

}

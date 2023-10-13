using Robust.Shared.GameStates;

namespace Content.Shared.Emp;

/// <summary>
/// Prevents EMP when attached to a grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedEmpSystem))]
public sealed partial class EmpImmuneGridComponent : Component
{

}

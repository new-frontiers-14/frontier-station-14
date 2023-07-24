namespace Content.Server.Emp;

/// <summary>
/// Upon being triggered will EMP area around it.
/// </summary>
[RegisterComponent]
[Access(typeof(EmpSystem))]
public sealed class EmpIgnoreComponent : Component
{
    [DataField("IgnoreEmp"), ViewVariables(VVAccess.ReadWrite)]
    public float IgnoreEmp = "true";
}

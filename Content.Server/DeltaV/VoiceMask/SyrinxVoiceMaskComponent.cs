namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class SyrinxVoiceMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)] public string VoiceName = "Unknown";
}

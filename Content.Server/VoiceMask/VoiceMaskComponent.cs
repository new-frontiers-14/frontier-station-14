namespace Content.Server.VoiceMask;

public enum Mode : byte // Frontier 
{
    Real,
    Fake,
    Unknown
}

[RegisterComponent]
public sealed partial class VoiceMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)] public string VoiceName = "Unknown";

    [ViewVariables(VVAccess.ReadWrite), DataField("mode")]
    [AutoNetworkedField]
    public Mode Mode = Mode.Unknown;
}

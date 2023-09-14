namespace Content.Server.Books
{
    [RegisterComponent]
    public sealed partial class HyperlinkBookComponent : Component
    {
        [DataField("url")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string URL = string.Empty;
    }
}

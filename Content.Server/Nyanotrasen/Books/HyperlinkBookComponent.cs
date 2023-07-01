namespace Content.Server.Books
{
    [RegisterComponent]
    public sealed class HyperlinkBookComponent : Component
    {
        [DataField("url")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string URL = string.Empty;
    }
}

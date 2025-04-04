namespace Content.Server._NF.Books.Components
{
    [RegisterComponent]
    public sealed partial class HyperlinkBookComponent : Component
    {
        [DataField("url")]
        public string URL = string.Empty;
    }
}


namespace Content.Server.Item.PseudoItem
{
    /// <summary>
    /// For entities that behave like an item under certain conditions,
    /// but not under most conditions.
    /// </summary>
    [RegisterComponent]
    public sealed class PseudoItemComponent : Component
    {
        [DataField("size")]
        public int Size = 120;

        public bool Active = false;
    }
}

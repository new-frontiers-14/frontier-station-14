
namespace Content.Server.SizeAttribute
{
    [RegisterComponent]
    public sealed partial class SizeAttributeComponent : Component
    {
        [DataField("short")]
        public bool Short = false;

        [DataField("tall")]
        public bool Tall = false;
    }
}

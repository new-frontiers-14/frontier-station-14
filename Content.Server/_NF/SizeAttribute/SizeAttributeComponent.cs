
using Content.Shared._NF.Cloning;

namespace Content.Server.SizeAttribute
{
    [RegisterComponent]
    public sealed partial class SizeAttributeComponent : Component, ITransferredByCloning
    {
        [DataField("short")]
        public bool Short = false;

        [DataField("tall")]
        public bool Tall = false;
    }
}

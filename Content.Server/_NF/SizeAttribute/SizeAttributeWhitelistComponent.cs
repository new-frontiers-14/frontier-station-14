using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.SizeAttribute
{
    [RegisterComponent]
    public sealed partial class SizeAttributeWhitelistComponent : Component
    {
        // Short
        [DataField("short")]
        public bool Short = false;

        [DataField("shortscale")]
        public float ShortScale = 0f;

        [DataField("shortDensity")]
        public float ShortDensity = 0f;

        // Tall
        [DataField("tall")]
        public bool Tall = false;

        [DataField("tallscale")]
        public float TallScale = 0f;

        [DataField("tallDensity")]
        public float TallDensity = 0f;
    }
}

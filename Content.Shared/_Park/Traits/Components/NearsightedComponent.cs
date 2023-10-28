using Robust.Shared.GameStates;

namespace Content.Shared._Park.Traits
{
    /// <summary>
    ///     Owner entity cannot see well, without prescription glasses.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class NearsightedComponent : Component
    {
        [DataField("radius"), ViewVariables(VVAccess.ReadWrite)]
        public float Radius = 0.8f;

        [DataField("alpha"), ViewVariables(VVAccess.ReadWrite)]
        public float Alpha = 0.995f;

        [DataField("gradius"), ViewVariables(VVAccess.ReadWrite)]
        public float gRadius = 0.45f;

        [DataField("galpha"), ViewVariables(VVAccess.ReadWrite)]
        public float gAlpha = 0.93f;

        [DataField("glasses"), ViewVariables(VVAccess.ReadWrite)]
        public bool Glasses = false;
    }
}

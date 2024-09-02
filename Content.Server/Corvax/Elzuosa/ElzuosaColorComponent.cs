using Robust.Shared.Serialization;

namespace Content.Server.Corvax.Elzuosa
{
    [RegisterComponent]
    public sealed partial class ElzuosaColorComponent : Component
    {
        public Color SkinColor { get; set; }

        public bool Hacked { get; set; } = false;

        [DataField("cycleRate")]
        public float CycleRate = 1f;

        [DataField("stannedByEMP")]
        public bool StannedByEmp = false;
    }

    /*[Serializable, NetSerializable]
    public enum ElzuosaState : byte
    {
        Normal,
        Emagged
    }*/
}

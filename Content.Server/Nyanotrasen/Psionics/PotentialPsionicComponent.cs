namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed partial class PotentialPsionicComponent : Component
    {
        [DataField("chance")]
        public float Chance = 0.04f;

        /// <summary>
        /// YORO (you only reroll once)
        /// </summary>
        public bool Rerolled = false;
    }
}

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class ClothingGrantPsionicPowerComponent : Component
    {
        [DataField("power", required: true)]
        public string Power = "";
        public bool IsActive = false;
    }
}
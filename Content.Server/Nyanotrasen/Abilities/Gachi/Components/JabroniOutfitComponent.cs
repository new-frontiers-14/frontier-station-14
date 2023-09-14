namespace Content.Server.Abilities.Gachi.Components
{
    [RegisterComponent]
    public sealed partial class JabroniOutfitComponent : Component
    {
        /// <summary>
        /// Is the component currently being worn and affecting someone?
        /// Making the unequip check not totally CBT
        /// </summary>
        public bool IsActive = false;
    }
}

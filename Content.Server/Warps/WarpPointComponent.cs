namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WarpPointComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("location")] public string? Location { get; set; }

        /// <summary>
        ///     If true, ghosts warping to this entity will begin following it.
        /// </summary>
        [DataField("follow")]
        public bool Follow = false;

        /// <summary>
        /// If true, will sync warp point name with a station name.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("useStationName")]
        public bool UseStationName;
    }
}

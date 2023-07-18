namespace Content.Server.M_Emp
{
    /// <summary>
    /// A grid spawned by a salvage magnet.
    /// </summary>
    [RegisterComponent]
    public sealed class M_EmpGridComponent : Component
    {
        /// <summary>
        /// The magnet that spawned this grid.
        /// </summary>
        public EntityUid? SpawnerMagnet;
    }
}

using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVar
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class EECCVars : CVars
    {
        #region Jetpack System

        /// <summary>
        ///     When true, Jetpacks can be enabled anywhere, even in gravity.
        /// </summary>
        public static readonly CVarDef<bool> JetpackEnableAnywhere =
            CVarDef.Create("ee.jetpack.enable_anywhere", false, CVar.REPLICATED);

        /// <summary>
        ///     When true, jetpacks can be enabled on grids that have zero gravity.
        /// </summary>
        public static readonly CVarDef<bool> JetpackEnableInNoGravity =
            CVarDef.Create("ee.jetpack.enable_in_no_gravity", true, CVar.REPLICATED);

        #endregion
    }
}

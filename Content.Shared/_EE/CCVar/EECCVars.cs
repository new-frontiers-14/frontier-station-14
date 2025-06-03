using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVar;

[CVarDefs] // ReSharper disable once InconsistentNaming
public sealed class EECCVars
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

    #region Contests System

    /// <summary>
    ///     The MASTER TOGGLE for the entire Contests System.
    ///     ALL CONTESTS BELOW, regardless of type or setting will output 1f when false.
    /// </summary>
    public static readonly CVarDef<bool> DoContestsSystem =
        CVarDef.Create("ee.contests.do_contests_system", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Contest functions normally include an optional override to bypass the clamp set by max_percentage.
    ///     This CVar disables the bypass when false, forcing all implementations to comply with max_percentage.
    /// </summary>
    public static readonly CVarDef<bool> AllowClampOverride =
        CVarDef.Create("ee.contests.allow_clamp_override", true, CVar.REPLICATED | CVar.SERVER);
    /// <summary>
    ///     Toggles all MassContest functions. All mass contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoMassContests =
        CVarDef.Create("ee.contests.do_mass_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all StaminaContest functions. All stamina contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoStaminaContests =
        CVarDef.Create("ee.contests.do_stamina_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all HealthContest functions. All health contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoHealthContests =
        CVarDef.Create("ee.contests.do_health_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MindContest functions. All mind contests output 1f when false.
    ///     MindContests are not currently implemented, and are awaiting completion of the Psionic Refactor
    /// </summary>
    public static readonly CVarDef<bool> DoMindContests =
        CVarDef.Create("ee.contests.do_mind_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MoodContest functions. All mood contests output 1f when false.
    /// </summary>
    public static readonly CVarDef<bool> DoMoodContests =
        CVarDef.Create("ee.contests.do_mood_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The maximum amount that Mass Contests can modify a physics multiplier, given as a +/- percentage
    ///     Default of 0.25f outputs between * 0.75f and 1.25f
    /// </summary>
    public static readonly CVarDef<float> MassContestsMaxPercentage =
        CVarDef.Create("ee.contests.max_percentage", 0.25f, CVar.REPLICATED | CVar.SERVER);

    #endregion
}

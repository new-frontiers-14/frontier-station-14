using Robust.Shared.Audio;

namespace Content.Shared._NF.SoftCrit;

/// <summary>
///     Mobs with this component will emote a deathgasp when they die.
/// </summary>
/// 
[RegisterComponent]
public sealed partial class SoftCritComponent : Component
{
    [DataField("unableToAct"), ViewVariables(VVAccess.ReadWrite)]
    public bool UnableToAct = false;

    /// <summary>
    /// Damage threshold at which mob will return to crit from softcrit
    /// </summary>
   [DataField("damageThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageThreshold = 150f;

    /// <summary>
    /// Speed to which mob will be changed when entering softcrit, to simulate crawl
    /// </summary>
   [DataField("crawlSprintSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float CrawlSprintSpeed = 1.25f;

    /// <summary>
    /// Speed to which mob will be changed when entering softcrit, to simulate crawl
    /// </summary>
   [DataField("crawlWalkSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float CrawlWalkSpeed = 0.75f;

    /// <summary>
    /// Stores speed at which mob moves when alive
    /// </summary>
   [DataField("baseSprintSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseSprintSpeed = 4.5f;

    /// <summary>
    /// Stores speed at which mob moves when alive
    /// </summary>
   [DataField("baseWalkSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseWalkSpeed = 2.5f;

    [DataField("crawlSound")]
    public SoundSpecifier CrawlSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");
}
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="NFPirateRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(NFPirateRuleSystem))]
public sealed partial class NFPirateRuleComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> PirateFaction = "NFPirate";
}

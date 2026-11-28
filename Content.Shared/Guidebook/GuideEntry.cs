using Content.Shared.Roles; // Frontier: guidebook department colors (Starlight #6822)
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Guidebook;

[Prototype]
public sealed partial class GuideEntryPrototype : GuideEntry, IPrototype
{
    public string ID => Id;
}

[Virtual]
public class GuideEntry
{
    /// <summary>
    ///     The file containing the contents of this guide.
    /// </summary>
    [DataField(required: true)] public ResPath Text = default!;

    /// <summary>
    ///     The unique id for this guide.
    /// </summary>
    [IdDataField]
    public string Id = default!;

    /// <summary>
    ///     The name of this guide. This gets localized.
    /// </summary>
    [DataField(required: true)] public string Name = default!;

    /// <summary>
    ///     The "children" of this guide for when guides are shown in a tree / table of contents.
    /// </summary>
    [DataField]
    public List<ProtoId<GuideEntryPrototype>> Children = new();

    /// <summary>
    ///     Enable filtering of items.
    /// </summary>
    [DataField] public bool FilterEnabled = default!;

    [DataField] public bool RuleEntry;

    /// <summary>
    ///     Priority for sorting top-level guides when shown in a tree / table of contents.
    ///     If the guide is the child of some other guide, the order simply determined by the order of children in <see cref="Children"/>.
    /// </summary>
    [DataField] public int Priority = 0;

    // Frontier: guidebook search and colors, adapted from Starlight #6822.
    /// <summary>
    /// Additional localized search terms, alongside the automatic
    /// <c>nf-guidebook-search-keywords-{lowercase Id}</c> localization entry.
    /// </summary>
    [DataField]
    public List<LocId> SearchKeywords = new();

    /// <summary>
    /// Contents color, inherited by descendants unless overridden.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// Department whose color is used when no explicit color is specified.
    /// </summary>
    [DataField]
    public ProtoId<DepartmentPrototype>? Department;
    // End Frontier
}

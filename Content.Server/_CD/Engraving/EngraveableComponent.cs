namespace Content.Server._CD.Engraving;

/// <summary>
///     Allows an items' description to be modified with an engraving
/// </summary>
[RegisterComponent, Access(typeof(EngraveableSystem))]
public sealed partial class EngraveableComponent : Component
{
    /// <summary>
    ///     Message given to user to notify them a message was sent
    /// </summary>
    [DataField]
    public string EngravedMessage = string.Empty;

    /// <summary>
    ///     The inspect text to use when there is no engraving
    /// </summary>
    [DataField]
    public LocId NoEngravingText = "engraving-generic-no-message"; // Frontier: "dogtags"<"generic"

    /// <summary>
    ///     The message to use when successfully engraving the item
    /// </summary>
    [DataField]
    public LocId EngraveSuccessMessage = "engraving-generic-succeed"; // Frontier: "dogtags"<"generic"

    /// <summary>
    ///     The inspect text to use when there is an engraving. The message will be shown seperately afterwards.
    /// </summary>
    [DataField]
    public LocId HasEngravingText = "engraving-generic-has-message"; // Frontier: "dogtags"<"generic"
}

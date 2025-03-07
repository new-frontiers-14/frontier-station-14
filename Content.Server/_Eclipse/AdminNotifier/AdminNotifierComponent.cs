namespace Content.Server._Eclipse.AdminNotifier;

/// <summary>
///     Allows an items' description to be modified with an engraving
/// </summary>
[RegisterComponent, Access(typeof(AdminNotifierSystem))]
public sealed partial class AdminNotifierComponent : Component
{
    /// <summary>
    ///     Message given to user to notify them a message was sent
    /// </summary>
    [DataField]
    public LocId AlertMessage = "admin-notifier-alert-message";
}

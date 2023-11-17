using System.Linq;
using Content.Shared.Shipyard;

namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ShuttleDeedComponent : Component
{
    public const int MaxPrefixLength = 6;
    public const int MaxNameLength = 30;
    public const int MaxSuffixLength = 3 + 1 + 4; // 3 digits, dash, up to 4 letters - should be enough

    [DataField("shuttleUid")]
    public EntityUid? ShuttleUid;

    [DataField("shuttlePrefix")]
    public string? ShuttleNamePrefix;

    [DataField("shuttleName")]
    public string? ShuttleName;

    [DataField("shuttleSuffix")]
    public string? ShuttleNameSuffix;

    [DataField("shuttleOwner")]
    public EntityUid? ShuttleOwner;

    /// <summary>
    /// Returns the full name of this shuttle in the form of [prefix] [name] [suffix].
    /// </summary>
    public string GetFullName()
    {
        return JoinNameParts(GetNameParts());
    }

    public string?[] GetNameParts()
    {
        return new[] { ShuttleNamePrefix, ShuttleName, ShuttleNameSuffix };
    }

    public static string JoinNameParts(string?[] parts)
    {
        return string.Join(' ', parts.Where(it => it != null));
    }
}

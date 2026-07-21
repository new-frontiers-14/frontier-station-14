using Robust.Shared.Network;

namespace Content.Shared._NF.Whitelist;

/// <summary>
/// A simple dependency to check if a specific user is globally whitelisted.
/// </summary>
/// <remarks>
/// This code should not have to exist, but due to how whitelisting implemented (not very well), there is no clean way to retrieve a user's
/// whitelist stats. That is why this has to exist. I hate that this has to exist, but it does. If you have the brass to
/// refactor the whitelist system so this class doesn't have to exist anymore, future coders will thank you.
/// </remarks>
public interface IGlobalWhitelistCheck
{
    /// <summary>
    /// Checks if a user has a global whitelist
    /// </summary>
    /// <param name="netUser">The user to check</param>
    /// <returns>Returns true if the user is whitelisted, and false if they aren't/there was an error looking it up.</returns>
    public abstract bool IsUserWhitelisted(NetUserId netUser);
}

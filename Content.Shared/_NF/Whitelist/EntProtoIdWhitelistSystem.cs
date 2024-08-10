using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Whitelist;

public sealed class EntProtoIdWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public bool IsValid(EntProtoIdWhitelist list, [NotNullWhen(true)] EntityUid? uid)
    {
        return uid != null && IsValid(list, uid.Value);
    }

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist.
    /// Note: no cycle checks, possibility to run away.
    /// </summary>
    public bool IsValid(EntProtoIdWhitelist list, EntityUid uid)
    {
        // All of the information we need is in EntityPrototype (ID, parents).
        NetEntity netEntity = _entMan.GetNetEntity(uid);
        MetaDataComponent? metadata = _entMan.GetComponentOrNull<MetaDataComponent>(_entMan.GetEntity(netEntity));
        EntityPrototype? nullableProto = metadata?.EntityPrototype;

        if (list.Ids is null || nullableProto is null)
            return false;

        EntityPrototype proto = nullableProto!;
        return IsPrototypeValid(list, proto);
    }

    // Recurse through parents: trust the list that the PrototypeManager returns.
    // Parents may be abstract, don't try to get an EntityPrototype from them.
    private bool IsValidRecursive(EntProtoIdWhitelist list, string prototypeId)
    {
        if (list.Ids is null)
            return false;

        foreach (var id in list.Ids)
        {
            if (prototypeId.Equals(id))
                return true;
        }

        // Nothing found here, recurse to the next set of parents.
        if (list.MatchParents)
        {
            foreach (var (parentId, _) in _protoMan.EnumerateAllParents<EntityPrototype>(prototypeId))
            {
                if (IsValidRecursive(list, parentId))
                    return true;
            }
        }

        return false;
    }

    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntProtoIdWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list
    /// </summary>
    public bool IsWhitelistPass(EntProtoIdWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is not on the list
    /// </summary>
    public bool IsWhitelistFail(EntProtoIdWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is on the list
    /// </summary>
    public bool IsWhitelistPassOrNull(EntProtoIdWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is not on the list
    /// </summary>
    public bool IsWhitelistFailOrNull(EntProtoIdWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is on list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPass(EntProtoIdWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPass(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFail(EntProtoIdWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFail(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPassOrNull(EntProtoIdWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPassOrNull(blacklist, uid);
    }

    /// <summary>                                        
    /// Helper function to determine if Blacklist is either null or the entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFailOrNull(EntProtoIdWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFailOrNull(blacklist, uid);
    }


    // Prototype variants

    /// <summary>
    /// Checks for a prototype
    /// </summary>
    /// <param name="list">The list to check</param>
    /// <param name="prototype">the prototype to check</param>
    /// <returns>True if it is valid</returns>
    public bool IsPrototypeValid(EntProtoIdWhitelist list, EntityPrototype prototype)
    {
        if (list.Ids is null)
            return false;

        // Check our prototype's ID against our desired list.  Any match is good (no duplicate IDs).
        foreach (var id in list.Ids)
        {
            if (prototype.ID.Equals(id))
                return true;
        }

        // If we haven't matched, check the parents if needed: recurse through each ancestor of this entity.
        if (list.MatchParents)
        {
            foreach (var (parentId, _) in _protoMan.EnumerateAllParents<EntityPrototype>(prototype.ID))
            {
                if (IsValidRecursive(list, parentId))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a given EntityPrototype passes the given whitelist
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistPass(EntProtoIdWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist != null && IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given whitelist
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistFail(EntProtoIdWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist != null && !IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given blacklist, or if the blacklist is null
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistPassOrNull(EntProtoIdWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist == null || IsPrototypeWhitelistPass(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype fails the given blacklist, or if the blacklist is null
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistFailOrNull(EntProtoIdWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist == null || IsPrototypeWhitelistFail(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given blacklist
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeBlacklistPass(EntProtoIdWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistPass(blacklist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype fails the given blacklist
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeBlacklistFail(EntProtoIdWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistFail(blacklist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given blacklist, or if the blacklist is null
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeBlacklistPassOrNull(EntProtoIdWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistPassOrNull(blacklist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype fails the given blacklist, or if the blacklist is null
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeBlacklistFailOrNull(EntProtoIdWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistFailOrNull(blacklist, prototype);
    }
}

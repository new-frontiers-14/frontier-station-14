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
        EntityPrototype? prototype = metadata?.EntityPrototype;

        if (list.Ids is null || prototype is null)
            return false;

        // Check our prototype's ID against our desired list.  Any match is good (no duplicate IDs).
        foreach (var id in list.Ids)
        {
            if (prototype?.ID?.Equals(id) ?? false)
                return true;
        }

        // If we haven't matched, check the parents if needed: recurse through each ancestor of this entity.
        if (list.MatchParents)
        {
            string[] parents = prototype?.Parents ?? new string[] {};
            foreach (var parent in parents)
            {
                if (IsValidRecursive(list, parent))
                    return true;
            }
        }

        return false;
    }

    private bool IsValidRecursive(EntProtoIdWhitelist list, string prototypeId)
    {
        // Check 
        if (!_protoMan.TryIndex(prototypeId, out var prototype))
            return false;

        if (list.Ids is null || prototype is null)
            return false;

        foreach (var id in list.Ids)
        {
            if (prototype?.ID?.Equals(id) ?? false)
                return true;
        }

        // Nothing found here, recurse to the next set of parents.
        if (list.MatchParents)
        {
            string [] parents = prototype?.Parents ?? new string[] {};
            foreach (var parent in parents)
            {
                if (IsValidRecursive(list, parent))
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
}

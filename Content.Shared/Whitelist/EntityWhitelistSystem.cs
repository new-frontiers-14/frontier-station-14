using System.Diagnostics.CodeAnalysis;
using System.Linq; // DeltaV
using Content.Shared.Item;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed class EntityWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    public override void Initialize()
    {
        base.Initialize();
        _itemQuery = GetEntityQuery<ItemComponent>();
    }

    /// <inheritdoc cref="IsValid(Content.Shared.Whitelist.EntityWhitelist,Robust.Shared.GameObjects.EntityUid)"/>
    public bool IsValid(EntityWhitelist list, [NotNullWhen(true)] EntityUid? uid)
    {
        return uid != null && IsValid(list, uid.Value);
    }

    /// <summary>
    /// Checks whether a given entity is allowed by a whitelist and not blocked by a blacklist.
    /// If a blacklist is provided and it matches then this returns false.
    /// If a whitelist is provided and it does not match then this returns false.
    /// If either list is null it does not get checked.
    /// </summary>
    public bool CheckBoth([NotNullWhen(true)] EntityUid? uid, EntityWhitelist? blacklist = null, EntityWhitelist? whitelist = null)
    {
        if (uid == null)
            return false;

        if (blacklist != null && IsValid(blacklist, uid))
            return false;

        return whitelist == null || IsValid(whitelist, uid);
    }

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, EntityUid uid)
    {
        if (list.Components != null)
        {
            var regs = StringsToRegs(list.Components);

            list.Registrations ??= new List<ComponentRegistration>();
            list.Registrations.AddRange(regs);
        }

        if (list.MindRoles != null)
        {
            var regs = StringsToRegs(list.MindRoles);

            foreach (var role in regs)
            {
                if ( _roles.MindHasRole(uid, role.Type, out _))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Registrations != null && list.Registrations.Count > 0)
        {
            foreach (var reg in list.Registrations)
            {
                if (HasComp(uid, reg.Type))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && _itemQuery.TryComp(uid, out var itemComp))
        {
            if (list.Sizes.Contains(itemComp.Size))
                return true;
        }

        if (list.Tags != null)
        {
            return list.RequireAll
                ? _tag.HasAllTags(uid, list.Tags)
                : _tag.HasAnyTag(uid, list.Tags);
        }

        return list.RequireAll;
    }

    /// <summary>
    /// FRONTIER ADDITION:
    /// Checks for a prototype
    /// </summary>
    /// <param name="list">The list to check</param>
    /// <param name="prototype">the prototype to check</param>
    /// <returns>True if it is valid</returns>
    public bool IsPrototypeValid(EntityWhitelist list, EntityPrototype prototype)
    {
        if (list.Components != null)
        {
            var regs = StringsToRegs(list.Components);

            list.Registrations ??= new List<ComponentRegistration>();
            list.Registrations.AddRange(regs);
        }

        // TODO - fix or deprecate mind role check with prototypes
        // if (list.MindRoles != null)
        // {
        //     var regs = StringsToRegs(list.MindRoles);

        //     foreach (var role in regs)
        //     {
        //         if (_roles.MindHasRole(uid, role.Type, out _))
        //         {
        //             if (!list.RequireAll)
        //                 return true;
        //         }
        //         else if (list.RequireAll)
        //             return false;
        //     }
        // }

        if (list.Registrations != null)
        {
            foreach (var reg in list.Registrations)
            {
                if (prototype.Components.ContainsKey(reg.Name) || prototype.Components.ContainsKey(reg.Type.ToString()))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && prototype.Components.TryGetComponent("Item", out var itemComp) && itemComp is ItemComponent component)
        {
            if (list.Sizes.Contains(component.Size))
                return true;
        }

        if (list.Tags != null)
        {
            if (prototype.Components.TryGetComponent("Tag", out var tagComponent) &&
                tagComponent is TagComponent comp)
            {
                return list.RequireAll
                    ? _tag.HasAllTags(comp, list.Tags)
                    : _tag.HasAnyTag(comp, list.Tags);
            }

        }

        return list.RequireAll;
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given whitelist
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistPass(EntityWhitelist? whitelist, EntityPrototype prototype)
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
    public bool IsPrototypeWhitelistFail(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist != null && !IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given whitelist, or if the whitelist is null
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistPassOrNull(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist == null || IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given whitelist, or if the whitelist is null
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeWhitelistFailOrNull(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist == null || !IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// FRONTIER ADDITION
    /// Checks if a given EntityPrototype passes the given blacklist
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns></returns>
    public bool IsPrototypeBlacklistPass(EntityWhitelist? blacklist, EntityPrototype prototype)
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
    public bool IsPrototypeBlacklistFail(EntityWhitelist? blacklist, EntityPrototype prototype)
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
    public bool IsPrototypeBlacklistPassOrNull(EntityWhitelist? blacklist, EntityPrototype prototype)
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
    public bool IsPrototypeBlacklistFailOrNull(EntityWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistFailOrNull(blacklist, prototype);
    }

    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntityWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        // Begin DeltaV
        var isValid = IsValid(whitelist, uid);
        Log.Debug($"Whitelist validation result for entity {ToPrettyString(uid)}: {isValid}");

        if (whitelist.RequireAll)
        {
            Log.Debug($"Whitelist requires all conditions - Components: {string.Join(", ", whitelist.Components ?? Array.Empty<string>())}, " +
                           $"Tags: {(whitelist.Tags != null ? string.Join(", ", whitelist.Tags.Select(t => t.ToString())) : "none")}");
        }

        return isValid;
        // EndDeltaV

        //return IsValid(whitelist, uid); // DeltaV
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is not on the list
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is on the list
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is not on the list
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is on list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPass(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPass(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFail(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFail(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPassOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPassOrNull(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFailOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFailOrNull(blacklist, uid);
    }

    private List<ComponentRegistration> StringsToRegs(string[]? input)
    {
        var list = new List<ComponentRegistration>();

        if (input == null || input.Length == 0)
            return list;

        foreach (var name in input)
        {
            var availability = _factory.GetComponentAvailability(name);
            if (_factory.TryGetRegistration(name, out var registration)
                && availability == ComponentAvailability.Available)
            {
                list.Add(registration);
            }
            else if (availability == ComponentAvailability.Unknown)
            {
                Log.Error($"StringsToRegs failed: Unknown component name {name} passed to EntityWhitelist!");
            }
        }

        return list;
    }
}

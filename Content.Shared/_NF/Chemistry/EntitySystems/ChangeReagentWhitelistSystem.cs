
using Content.Shared._NF.Chemistry.Components;
using Content.Shared._NF.Chemistry.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared._NF.Chemistry.EntitySystems;

/// <summary>
///     Allows an entity to change an injector component's whitelist via a UI box
/// </summary>
public sealed class ReagentWhitelistChangeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [NetSerializable, Serializable]
    public enum ReagentWhitelistChangeUIKey : byte
    {
        Key
    }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentWhitelistChangeComponent, GetVerbsEvent<InteractionVerb>>(AddChangeFilterVerb);
        SubscribeLocalEvent<ReagentWhitelistChangeComponent, ReagentWhitelistChangeMessage>(OnReagentWhitelistChange);
        SubscribeLocalEvent<ReagentWhitelistChangeComponent, ReagentWhitelistResetMessage>(OnReagentWhitelistReset);
    }

    private void AddChangeFilterVerb(Entity<ReagentWhitelistChangeComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        var @event = args;
        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("comp-change-reagent-whitelist-verb-filter"),
            //Icon = new SpriteSpecifier(),
            Act = () =>
            {
                _ui.OpenUi(uid, ReagentWhitelistChangeUIKey.Key, @event.User);
            },
            Priority = 1
        });
    }

    private void OnReagentWhitelistChange(Entity<ReagentWhitelistChangeComponent> ent, ref ReagentWhitelistChangeMessage args)
    {
        if (!TryComp<InjectorComponent>(ent.Owner, out var injectorComp))
        {
            return;
        }

        if (!_prototypeManager.TryIndex(args.NewReagentProto, out var protoComp))
        {
            return;
        }

        if (!ent.Comp.AllowedReagentGroups.Contains(protoComp.Group))
        {
            return;
        }

        injectorComp.ReagentWhitelist = new[] { args.NewReagentProto };
    }

    private void OnReagentWhitelistReset(Entity<ReagentWhitelistChangeComponent> ent, ref ReagentWhitelistResetMessage args)
    {
        if (!TryComp<InjectorComponent>(ent.Owner, out var injectorComp))
        {
            return;
        }

        injectorComp.ReagentWhitelist = null;
    }
}

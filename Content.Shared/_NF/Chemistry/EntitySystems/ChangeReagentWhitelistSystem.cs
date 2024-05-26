
using Content.Shared._NF.Chemistry.Components;
using Content.Shared._NF.Chemistry.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;


namespace Content.Shared._NF.Chemistry.EntitySystems;

/// <summary>
///     Allows an entity to change an injector component's whitelist via a UI box
/// </summary>
public sealed class ReagentWhitelistChangeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
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
    }

    private void AddChangeFilterVerb(Entity<ReagentWhitelistChangeComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        var @event = args;
        args.Verbs.Add(new InteractionVerb()
        {
            // For debuging purposes only, replace before merging
            Text = "Set Reagent Filter",
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
        if (!TryComp<InjectorComponent>(ent.Owner, out var injectorComp) || injectorComp.ReagentWhitelist is null)
        {
            return;
        }

        if (!ent.Comp.AllowedReagentGroups.Contains(args.NewReagentProto))
        {
            return;
        }
        injectorComp.ReagentWhitelist.Clear();
        injectorComp.ReagentWhitelist.Add(args.NewReagentProto);
    }
}

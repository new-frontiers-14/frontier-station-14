using Content.Shared.Radio.Components;
using Robust.Server.GameObjects;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Content.Shared.Radio;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     Skibidi Code?
/// </summary>
public sealed partial class ShuttleIntercomSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShuttleIntercomComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    private void OnAlternativeVerb(EntityUid uid, ShuttleIntercomComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var openUiVerb = new AlternativeVerb
        {
            Act = () => ToggleUi(uid, component, args.User),
            Text = Loc.GetString("intercom-verb")
        };
        args.Verbs.Add(openUiVerb);
    }

    private void ToggleUi(EntityUid uid, ShuttleIntercomComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(uid, IntercomUiKey.Key, actor.PlayerSession);
    }
}

using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Card;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, GetVerbsEvent<AlternativeVerb>>(AddTurnOnVerb);
        SubscribeLocalEvent<CardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CardComponent, UseInHandEvent>(OnUse);

    }

    private void OnExamined(EntityUid uid, CardComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !component.Flipped)
        {
            args.PushMarkup(Loc.GetString("card-examined", ("target",  Loc.GetString(component.Name))));
        }
    }

    private void AddTurnOnVerb(EntityUid uid, CardComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => FlipCard(uid, component),
            Text = Loc.GetString("cards-verb-flip"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 1
        });
    }

    private void OnUse(EntityUid uid, CardComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        FlipCard(uid, comp);
        args.Handled = true;
    }


    /// <summary>
    /// Server-Side only method to flip card. This starts CardFlipUpdatedEvent event
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void FlipCard(EntityUid uid, CardComponent component)
    {
        if (_net.IsClient)
            return;
        component.Flipped = !component.Flipped;
        Dirty(uid, component);
        RaiseNetworkEvent(new CardFlipUpdatedEvent(GetNetEntity(uid)));
    }
}

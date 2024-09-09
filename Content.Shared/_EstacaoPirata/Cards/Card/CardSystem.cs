using Content.Shared._EstacaoPirata.Cards.Deck;
using Content.Shared._EstacaoPirata.Cards.Hand;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Card;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly CardStackSystem _cardStack = default!;
    [Dependency] private readonly CardDeckSystem _cardDeck = default!;
    [Dependency] private readonly CardHandSystem _cardHand = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
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

        if (args.Using == null || args.Using == args.Target)
            return;

        if (TryComp<CardStackComponent>(args.Using, out var usingStack))
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => JoinCards(args.User, args.Target, component, (EntityUid)args.Using, usingStack),
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 2
            });
        }
        else if (TryComp<CardComponent>(args.Using, out var usingCard))
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => _cardHand.TrySetupHandOfCards(args.User, args.Target, component, args.Using.Value, usingCard, false),
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 2
            });
        }
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

    private void JoinCards(EntityUid user, EntityUid first, CardComponent firstComp, EntityUid second, CardStackComponent secondStack)
    {
        if (_net.IsClient)
            return;

        EntityUid cardStack;
        bool? flip = null;
        if (HasComp<CardDeckComponent>(second))
        {
            cardStack = SpawnInSameParent(_cardDeck.CardDeckBaseName, first);
        }
        else if (HasComp<CardHandComponent>(second))
        {
            cardStack = SpawnInSameParent(_cardHand.CardHandBaseName, first);
            if(TryComp<CardHandComponent>(cardStack, out var stackHand))
                stackHand.Flipped = firstComp.Flipped;
            flip = firstComp.Flipped;
        }
        else
            return;

        if (!TryComp(cardStack, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryInsertCard(cardStack, first, stack))
            return;
        _cardStack.TransferNLastCardFromStacks(user, secondStack.Cards.Count, second, secondStack, cardStack, stack);
        if (flip != null)
            _cardStack.FlipAllCards(cardStack, stack, flip); //???
    }

    // Frontier: tries to spawn an entity with the same parent as another given entity.
    //           Useful when spawning decks/hands in a backpack, for example.
    private EntityUid SpawnInSameParent(EntProtoId prototype, EntityUid uid)
    {
        if (_container.IsEntityOrParentInContainer(uid) &&
            _container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            return SpawnInContainerOrDrop(prototype, container.Owner, container.ID);
        }
        return Spawn(prototype, Transform(uid).Coordinates);
    }
}

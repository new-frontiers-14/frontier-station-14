using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Card;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Hand;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardHandSystem : EntitySystem
{
    const string CardHandBaseName = "CardHandBase";
    const string CardDeckBaseName = "CardDeckBase";

    [Dependency] private readonly CardStackSystem _cardStack = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;




    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CardHandComponent, CardHandDrawMessage>(OnCardDraw);
        SubscribeLocalEvent<CardHandComponent, CardStackQuantityChangeEvent>(OnStackQuantityChange);
        SubscribeLocalEvent<CardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    private void OnStackQuantityChange(EntityUid uid, CardHandComponent comp,  CardStackQuantityChangeEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(uid, out CardStackComponent? stack))
            return;

        var text = args.Type switch
        {
            StackQuantityChangeType.Added => "cards-stackquantitychange-added",
            StackQuantityChangeType.Removed => "cards-stackquantitychange-removed",
            _ => "cards-stackquantitychange-unknown"
        };

        _popupSystem.PopupEntity(Loc.GetString(text, ("quantity", stack.Cards.Count)), uid);

        _cardStack.FlipAllCards(uid, stack, false);
    }

    private void OnCardDraw(EntityUid uid, CardHandComponent comp, CardHandDrawMessage args)
    {
        if (!TryComp(uid, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryRemoveCard(uid, GetEntity(args.Card), stack))
            return;

        _hands.TryPickupAnyHand(args.Actor, GetEntity(args.Card));


        if (stack.Cards.Count != 1)
            return;
        var lastCard = stack.Cards.Last();
        if (!_cardStack.TryRemoveCard(uid, lastCard, stack))
            return;
        _hands.TryPickupAnyHand(args.Actor, lastCard);

    }

    private void OpenHandMenu(EntityUid user, EntityUid hand)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.OpenUi(hand, CardUiKey.Key, actor.PlayerSession);

    }

    private void OnAlternativeVerb(EntityUid uid, CardHandComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => OpenHandMenu(args.User, uid),
            Text = Loc.GetString("cards-verb-pickcard"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Priority = 3
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => ConvertToDeck(args.User, uid),
            Text = Loc.GetString("cards-verb-convert-to-deck"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
            Priority = 2
        });
    }

    private void OnInteractUsing(EntityUid uid, CardComponent comp, InteractUsingEvent args)
    {
        if (TryComp(args.Used, out CardComponent? usedComp) && TryComp(args.Target, out CardComponent? targetComp))
        {
            TrySetupHandOfCards(args.User, args.Used, usedComp, args.Target, targetComp);
        }
    }

    private void ConvertToDeck(EntityUid user, EntityUid hand)
    {
        if (_net.IsClient)
            return;

        var cardDeck = Spawn(CardDeckBaseName, Transform(hand).Coordinates);

        bool isHoldingCards = _hands.IsHolding(user, hand);

        EnsureComp<CardStackComponent>(cardDeck, out var deckStack);
        if (!TryComp(hand, out CardStackComponent? handStack))
            return;
        _cardStack.TryJoinStacks(cardDeck, hand, deckStack, handStack);

        if (isHoldingCards)
            _hands.TryPickupAnyHand(user, cardDeck);
    }
    private void TrySetupHandOfCards(EntityUid user, EntityUid card, CardComponent comp, EntityUid target, CardComponent targetComp)
    {
        if (_net.IsClient)
            return;
        var cardHand = Spawn(CardHandBaseName, Transform(card).Coordinates);
        if (!TryComp(cardHand, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryInsertCard(cardHand, card, stack) || !_cardStack.TryInsertCard(cardHand, target, stack))
            return;
        if (!_hands.TryPickupAnyHand(user, cardHand))
            return;
        _cardStack.FlipAllCards(cardHand, stack, false);
    }





}

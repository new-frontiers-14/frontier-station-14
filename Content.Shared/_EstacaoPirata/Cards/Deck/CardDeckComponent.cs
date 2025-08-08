using Robust.Shared.Audio;

namespace Content.Shared._EstacaoPirata.Cards.Deck;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CardDeckComponent : Component
{
    [DataField]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    [DataField]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    [DataField]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");

    [DataField]
    public float YOffset = 0.02f;

    [DataField]
    public float Scale = 1;

    [DataField]
    public int CardLimit = 5;
}

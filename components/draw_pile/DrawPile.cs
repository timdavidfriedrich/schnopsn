namespace Schnopsn.components.draw_pile;

using Godot;
using Schnopsn.components.card;
using System.Collections.Generic;


public partial class DrawPile : Node2D
{
    private readonly List<Card> _cards = [];
    
    private const float _maxRotation = 0.2f;
    private const float _cardOffsetX = 0.5f;
    private const float _cardOffsetY = 0.3f;
    
    private RandomNumberGenerator _random = new();

    public override void _Ready()
    {
        _random.Randomize();
    }

    public void AddCard(Card card)
    {
        card.GetParent()?.RemoveChild(card);
        AddChild(card);
        
        int cardIndex = _cards.Count;
        Vector2 offset = new(
            cardIndex * _cardOffsetX,
            cardIndex * _cardOffsetY
        );
        
        float randomRotation = _random.Randf() * _maxRotation - (_maxRotation / 2f);
        
        card.GlobalPosition = GlobalPosition + offset;
        card.Rotation = randomRotation;
        
        _cards.Add(card);
    }

    public Card DrawCard()
    {
        if (_cards.Count == 0) return null;

        Card topCard = _cards[_cards.Count - 1];
        _cards.RemoveAt(_cards.Count - 1);

        if (topCard.GetParent() == this)
        {
            RemoveChild(topCard);
        }
        return topCard;
    }

    public int GetCardCount() => _cards.Count;
}

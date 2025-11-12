namespace Schnopsn.components.hand;

using Godot;
using Schnopsn.components.card;
using System.Collections.Generic;
using System.Linq;
using static Godot.Control;

public partial class Hand : Node2D
{
    [Signal]
    public delegate void WantsToPlayCardEventHandler(Card card);

    [Export]
    private HBoxContainer _cardContainer;
    private Card _selectedCard = null;
    private Dictionary<Card, Control> _cardPlaceholders = [];

    private Vector2 CardSize = new(59, 92);

    public void AddCard(Card card)
    {
        Control placeholder = new();
        _cardContainer.AddChild(placeholder);
        placeholder.CustomMinimumSize = CardSize;

        AddChild(card);
        card.Placeholder = placeholder;

        _cardPlaceholders.Add(card, placeholder);
        card.Clicked += OnCardClicked;
    }

    public void RemoveCard(Card card)
    {
        if (_cardPlaceholders.TryGetValue(card, out Control placeholder))
        {
            placeholder.QueueFree();
            _cardPlaceholders.Remove(card);
            card.Clicked -= OnCardClicked;
        }
    }

    private void OnCardClicked(Card clickedCard)
    {
        if (clickedCard.State == CardState.InHand)
        {
            _selectedCard?.Deselect();
            _selectedCard = clickedCard;
            _selectedCard.Select();
        }
        else if (clickedCard.State == CardState.Selected)
        {
            _selectedCard = null;
            RemoveCard(clickedCard);
            EmitSignal(SignalName.WantsToPlayCard, clickedCard);
        }
    }

    public void OnTouchOutside()
    {
        _selectedCard?.Deselect();
        _selectedCard = null;
    }

    public IReadOnlyList<Card> GetCards()
    {
        // Die Keys im Dictionary sind genau die Karten in der Hand
        return _cardPlaceholders.Keys.ToList();
    }

    // (optional, praktisch)
    public int Count => _cardPlaceholders.Count;

    public void SetInteractive(bool enabled)
    {
        foreach (var card in _cardPlaceholders.Keys)
        {
            card.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            card.Modulate = enabled ? Colors.White : new Color(1, 1, 1, 0.6f);
        }
    }
}
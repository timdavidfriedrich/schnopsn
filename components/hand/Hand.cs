namespace Schnopsn.components.hand;

using Godot;
using Schnopsn.components.card;
using System.Collections.Generic;

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


    // DEBUGGING
    public override void _Draw()
    {
        ColorRect colorRect = _cardContainer.GetChildOrNull<ColorRect>(0);
        HBoxContainer cardContainer = GetNode<HBoxContainer>("CardContainer");
        if (colorRect != null)
        {
            var rect = new Rect2(cardContainer.Position, cardContainer.Size);
            DrawRect(rect, Colors.Red, filled: false, width: 2.0f);
        }
    }

    public override void _Ready()
    {
        ColorRect colorRect = _cardContainer.GetChildOrNull<ColorRect>(0);
        if (colorRect != null)
        {
            colorRect.Resized += QueueRedraw;
        }
    }
}

namespace Schnopsn.components.hand;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System.Collections.Generic;

public partial class Hand : CardReceiver
{
    [Signal]
    public delegate void WantsToPlayCardEventHandler(Card card);

    [Export]
    private bool _isPlayerHand = false;
    [Export]
    private HBoxContainer _cardContainer;
    private Card _selectedCard = null;
    private Dictionary<Card, Control> _cardPlaceholders = [];

    private Vector2 CardSize = new(59, 92);

    protected override Vector2 GetTargetPosition(Card card)
    {
        if (_cardPlaceholders.TryGetValue(card, out Control placeholder))
        {
            return placeholder.GlobalPosition;
        }
        return GlobalPosition;
    }

    public override void ReceiveCard(Card card)
    {
        Control placeholder = new();
        _cardContainer.AddChild(placeholder);
        placeholder.CustomMinimumSize = CardSize;
        
        _cardPlaceholders.Add(card, placeholder);

        base.ReceiveCard(card);

        CardPositioned += (receivedCard) => 
        {
            if (receivedCard == card)
            {
                FinalizeCardInHand(card, placeholder);
            }
        };
    }

    private void FinalizeCardInHand(Card card, Control placeholder)
    {
        card.Placeholder = placeholder;
        card.State = CardState.InHand;
        if (_isPlayerHand)
        {
            card.FaceUp();
        }
        // TODO: Make click behaviour to player only (=> inside if)
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
}
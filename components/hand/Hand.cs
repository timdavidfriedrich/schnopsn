namespace Schnopsn.components.hand;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System.Collections.Generic;
using System.Linq;

public partial class Hand : CardReceiver
{
    [Signal]
    public delegate void WantsToPlayCardEventHandler(Card card, Hand hand);

    [Export]
    private bool _isPlayerHand = false;

    [Export]
    private bool _debugMode = false;

    [Export]
    private HBoxContainer _cardContainer;
    
    [Export]
    private float _heightArch = 10.0f;
    [Export(PropertyHint.Range, "0, 20")]
    private float _fanSpreadAngle = 5.0f;

    [Export(PropertyHint.Range, "1.0, 5.0")]
    private float _handArcCurve = 2.0f;

    private Card _selectedCard = null;
    private Dictionary<Card, Control> _cardPlaceholders = [];

    private Vector2 CardSize = new(59, 92);


    public override void _Process(double delta)
    {
        HandleCardPositions((float)delta);
        HandleCardRotations((float)delta);
    }

    private void HandleCardRotations(float delta)
    {
        int totalCards = _cardPlaceholders.Count;
        if (totalCards == 0) return;

        float centerIndex = (totalCards - 1) / 2.0f;

        foreach (var kvp in _cardPlaceholders)
        {
            Card card = kvp.Key;
            Control placeholder = kvp.Value;

            // * Skip cards that haven't settled yet
            if (card.State == CardState.Transitioning) continue;

            if (card.State == CardState.Selected)
            {
                card.RotationDegrees = Mathf.Lerp(card.RotationDegrees, 0, delta * 15f);
                card.PivotOffset = card.PivotOffset.Lerp(card.Size / 2.0f, delta * 15f);
                continue;
            }

            if (card.State != CardState.Idle) continue;

            int cardIndex = placeholder.GetIndex();
            float distFromCenter = cardIndex - centerIndex;

            Vector2 targetPivot = new Vector2(
                card.Size.X / 2.0f, 
                card.Size.Y * _handArcCurve
            );

            float targetRotation = distFromCenter * _fanSpreadAngle;
            if (!_isPlayerHand)
            {
                targetRotation = -targetRotation;
            }

            card.PivotOffset = card.PivotOffset.Lerp(targetPivot, delta * 10f);
            card.RotationDegrees = Mathf.Lerp(card.RotationDegrees, targetRotation, delta * 10f);
        }
    }

    private void HandleCardPositions(float delta)
    {
        foreach (var kvp in _cardPlaceholders)
        {
            Card card = kvp.Key;
            // * Only reposition cards that are settled in the hand
            if (card.State != CardState.Idle && card.State != CardState.Selected) continue;
            Vector2 targetPos = GetTargetPosition(card);
            card.GlobalPosition = card.GlobalPosition.Lerp(targetPos, delta * 15f);
        }
    }

    protected override Vector2 GetTargetPosition(Card card)
    {
        if (_cardPlaceholders.TryGetValue(card, out Control placeholder))
        {
            Vector2 targetPos = placeholder.GlobalPosition;
            int count = _cardPlaceholders.Count;
            if (count > 1)
            {
                float centerIndex = (count - 1) / 2.0f;
                int myIndex = placeholder.GetIndex();
                float distFromCenter = Mathf.Abs(myIndex - centerIndex);
                float yOffset = distFromCenter * distFromCenter * (_heightArch / 2.0f);
                if (!_isPlayerHand)
                {
                    yOffset = -yOffset;
                }
                targetPos.Y += yOffset;
            }
            return targetPos;
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
        card.State = CardState.Idle;
        if (_isPlayerHand || _debugMode)
        {
            card.FaceUp();
        }
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
        if (clickedCard.State == CardState.Idle)
        {
            _selectedCard?.Deselect();
            _selectedCard = clickedCard;
            _selectedCard.Select();
        }
        else if (clickedCard.State == CardState.Selected)
        {
            _selectedCard = null;
            RemoveCard(clickedCard);
            EmitSignal(SignalName.WantsToPlayCard, clickedCard, this);
        }
    }

    public void OnTouchOutside()
    {
        _selectedCard?.Deselect();
        _selectedCard = null;
    }

    public bool containsCard(Card card)
    {
        return _cardPlaceholders.ContainsKey(card);
    }

    public bool CheckAnsage(Card card)
    {
        if (card == null) return false;
        if (card.Value != CardValue.koenig && card.Value != CardValue.ober) return false;

        var possibleAnsagen = _cardPlaceholders.Keys
            .Where(c => !ReferenceEquals(c, card)
                        && c.Color == card.Color
                        && (c.Value == CardValue.ober || c.Value == CardValue.koenig));

        if (card.Value == CardValue.koenig)
        {
            return possibleAnsagen.Any(c => c.Value == CardValue.ober);
        }
        else // ober
        {
            return possibleAnsagen.Any(c => c.Value == CardValue.koenig);
        }
    }

    public bool HasCards => _cardPlaceholders.Count > 0;

    public void PlayAnyCard()
    {
        if (_cardPlaceholders.Count == 0) return;

        var card = _cardPlaceholders.Keys.FirstOrDefault(c => c.State == CardState.Idle);
        if (card == null) return;
        RemoveCard(card);
        EmitSignal(SignalName.WantsToPlayCard, card, this);
    }
}
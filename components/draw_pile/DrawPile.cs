namespace Schnopsn.components.draw_pile;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System.Collections.Generic;

public partial class DrawPile : CardReceiver
{
	private readonly List<Card> _cards = [];

	private const float _maxRotationDegrees = 12f;
	private const float _cardOffsetX = 0.5f;
	private const float _cardOffsetY = 0.3f;
	public int CardCount => _cards.Count;


	private RandomNumberGenerator _random = new();

	public override void _Ready()
	{
		_random.Randomize();
	}

	public override void ReceiveCard(Card card)
	{
		_cards.Add(card);

		base.ReceiveCard(card);

		void OnCardPositionedHandler(Card receivedCard)
		{
			if (receivedCard == card)
			{
				ApplyPilePositioning(card);
				CardPositioned -= OnCardPositionedHandler;
			}
		}

		CardPositioned += OnCardPositionedHandler;
	}

	public bool ContainsCard(Card card)
	{
		return _cards.Contains(card);
	}
	
	public void RemoveCard(Card card)
	{
		_cards.Remove(card);
	}

	private void ApplyPilePositioning(Card card)
	{
		int cardIndex = _cards.IndexOf(card);
		if (cardIndex == -1) return;
		
		Vector2 offset = new(
			cardIndex * _cardOffsetX,
			cardIndex * _cardOffsetY
		);

		card.GlobalPosition = GlobalPosition + offset;

		if (cardIndex == 0) {
			card.FaceUp();
			Add90DegreeRotation(card);
		}
		else
        {
			card.FaceDown();
			AddRandomRotation(card);
        }
	}

	private void Add90DegreeRotation(Card card)
	{
		float minus90Degrees = -90f;
		Vector2 cardRightSideOffset = new(card.Size.X, (card.Size.Y - card.Size.X) / 2);
		AddRotation(card, minus90Degrees, cardRightSideOffset);
	}

	private void AddRandomRotation(Card card)
    {
		float randomDegrees = _random.Randf() * _maxRotationDegrees - (_maxRotationDegrees / 2f);
		Vector2 cardCenter = new(card.Size.X / 2, card.Size.Y / 2);
		AddRotation(card, randomDegrees, cardCenter);
    }

	private void AddRotation(Card card, float rotation, Vector2? pivotOffset = null)
	{
		card.PivotOffset = pivotOffset ?? new Vector2(card.Size.X / 2, card.Size.Y / 2);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(card, "rotation_degrees", rotation, 0.3)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	public Card DrawCard()
	{
		if (_cards.Count == 0) return null;

		int topCardIndex = _cards.Count -1;
		Card topCard = _cards[topCardIndex];
		_cards.RemoveAt(topCardIndex);
		
		if (topCard.IsFaceUp)
        {
			topCard.FaceDown();
			AddRandomRotation(topCard);
        }

		return topCard;
	}
}

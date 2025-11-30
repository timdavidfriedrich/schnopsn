namespace Schnopsn.components.draw_pile;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System.Collections.Generic;

public partial class DrawPile : CardReceiver
{
	private readonly List<Card> _cards = [];

	private const float _cardOffsetX = 0.5f;
	private const float _cardOffsetY = 0.3f;
	public int CardCount => _cards.Count;


	public override void ReceiveCard(Card card)
	{
		_cards.Add(card);

		base.ReceiveCard(card);

		void OnCardPositionedHandler(Card receivedCard)
		{
			if (receivedCard == card)
			{
				ApplyPilePositioning(card);
				card.State = CardState.Idle;
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
			card.Add90DegreeRotation();
		}
		else
        {
			card.FaceDown();
			card.AddRandomRotation();
        }
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
			topCard.AddRandomRotation();
        }

		return topCard;
	}

	public Card PeekBottomCard()
	{
		if (_cards.Count == 0)
			return null;

		return _cards[0]; // unterste Karte im Stapel
	}

}

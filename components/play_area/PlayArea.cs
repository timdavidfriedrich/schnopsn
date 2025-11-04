namespace Schnopsn.components.play_area;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System.Collections.Generic;

public partial class PlayArea : CardReceiver
{
    [Signal]
    public delegate void BothCardsPlayedEventHandler(Card[] cards);

    private List<Card> _cardsInPlay = [];
    
    public int GetCardCount() => _cardsInPlay.Count;

    protected override Vector2 GetTargetPosition(Card card)
    {
        int cardIndex = _cardsInPlay.IndexOf(card);
        if (cardIndex == -1)
        {
            cardIndex = _cardsInPlay.Count;
        }

        float cardWidth = card.Size.X;
        float xOffset = (cardIndex == 0) ? -cardWidth / 2f : cardWidth / 2f;
        
        return GlobalPosition + new Vector2(xOffset, 0);
    }

    public override void ReceiveCard(Card card)
    {
        _cardsInPlay.Add(card);
        base.ReceiveCard(card);
        
        CardPositioned += (receivedCard) => 
        {
            if (receivedCard == card)
            {
                FinalizeCardInPlayArea(card);
            }
        };
    }

    private void FinalizeCardInPlayArea(Card card)
    {
        card.FaceUp();
        if (_cardsInPlay.Count == 2)
        {
            EmitSignal(SignalName.BothCardsPlayed, _cardsInPlay.ToArray());
            _cardsInPlay.Clear();
        }
    }
}
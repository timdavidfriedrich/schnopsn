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

    public override void ReceiveCard(Card card)
    {
        base.ReceiveCard(card);
        _cardsInPlay.Add(card);
        FinalizeCardInPlayArea(card);
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

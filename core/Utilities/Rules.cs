using System.Drawing;
using Schnopsn;
using Schnopsn.components.card;
using Schnopsn.components.hand;

namespace Schnopsn.core.Utilities
{
    public static class Rules
    {
        public static int Points(CardValue v) => v switch
        {
            CardValue.sau => 11,
            CardValue.zehn => 10,
            CardValue.koenig => 4,
            CardValue.ober => 3,
            CardValue.unter => 2,
            _ => 0
        };

        public static int Rank(CardValue v) => v switch
        {
            CardValue.sau => 5,
            CardValue.zehn => 4,
            CardValue.koenig => 3,
            CardValue.ober => 2,
            CardValue.unter => 1,
            _ => 0
        };

        public static Card determineWinner(Card firstCard, Card secondCard, CardColor trumpColor)
        {
            if (secondCard.Color == firstCard.Color)
            {
                return Rank(secondCard.Value) > Rank(firstCard.Value) ? secondCard : firstCard;
            }
            else if (secondCard.Color == trumpColor && firstCard.Color != trumpColor)
            {
                return secondCard;
            }
            else
            {
                return firstCard;
            }
        }
        
    }
}
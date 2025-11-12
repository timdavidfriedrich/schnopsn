using System;
using System.Linq;
using System.Collections.Generic;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;

namespace Schnopsn.core
{
    public static class SimpleAi
    {
        private static Card Lowest(IEnumerable<Card> cards)
            => cards.OrderBy(c => Rules.Rank(c.Value)).First();

        private static Card Highest(IEnumerable<Card> cards)
            => cards.OrderByDescending(c => Rules.Rank(c.Value)).First();

        // Entscheidung im offenen Spiel (kein Zwang):
        // - Wenn Vorhand: spiel eher eine kleine Nicht-Trumpf-Karte (Bube/Dame),
        //   sonst kleinste Trumpf.
        // - Wenn Nachhand: 
        //   * Falls Gegner hohe Punkte (Ass/Zehn) gelegt und wir mit Trumpf stechen können -> steche.
        //   * Sonst, wenn wir die Farbe höher schlagen können -> tue das.
        //   * Ansonsten wirf die kleinste Karte ab (gern Nicht-Trumpf).
        public static Card ChooseCardOpenTalon(
            IReadOnlyList<Card> hand, 
            CardColor trump, 
            Card? leadCard // null, wenn KI ausspielt
        )
        {
            var cards = hand.ToList();

            if (leadCard == null)
            {
                // Vorhand: kleine Nicht-Trumpf-Karte bevorzugen
                var nonTrump = cards.Where(c => c.Color != trump);
                if (nonTrump.Any())
                {
                    // Bevorzugt Bube/Dame
                    var smallPref = nonTrump
                        .OrderBy(c => Rules.Rank(c.Value))
                        .ThenBy(c => c.Color == trump ? 1 : 0);
                    return smallPref.First();
                }
                // sonst: kleinster Trumpf
                return Lowest(cards);
            }
            else
            {
                // Nachhand: auf gegnerische Karte reagieren
                var leadColor = leadCard.Color;
                bool leadIsBig = (leadCard.Value == CardValue.sau || leadCard.Value == CardValue.zehn);

                var sameColorHigher = cards
                    .Where(c => c.Color == leadColor && Rules.Rank(c.Value) > Rules.Rank(leadCard.Value));
                if (sameColorHigher.Any())
                {
                    // Schlage mit kleiner, aber ausreichender höheren Karte
                    return sameColorHigher.OrderBy(c => Rules.Rank(c.Value)).First();
                }

                // Trumpfen, wenn Gegner hohe Punkte legte oder generell sinnvoll
                var trumps = cards.Where(c => c.Color == trump);
                if (trumps.Any())
                {
                    if (leadIsBig)
                    {
                        // nimm den kleinsten Trumpf, der sticht (im offenen Spiel sticht jeder Trumpf)
                        return trumps.OrderBy(c => Rules.Rank(c.Value)).First();
                    }
                }

                // ansonsten billig abwerfen (Nicht-Trumpf bevorzugt)
                var nonTrump = cards.Where(c => c.Color != trump);
                if (nonTrump.Any()) return Lowest(nonTrump);
                return Lowest(cards);
            }
        }
    }
}

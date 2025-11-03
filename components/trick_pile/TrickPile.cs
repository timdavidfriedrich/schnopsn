namespace Schnopsn.components.trick_pile;

using Godot;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using System;

public partial class TrickPile : CardReceiver
{
    public override void ReceiveCard(Card card)
    {
        base.ReceiveCard(card);
        if (GetChildCount() <= 2)
        {
            RotateAndFlipCard(card);
        }
        else
        {
            card.FaceDown();
        }
    }

    private void RotateAndFlipCard(Card card)
    {
        card.FaceUp();
        card.RotationDegrees = 90;
        card.Position += new Vector2(20, 10);
    }
}

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
        int index = GetChildCount();

        if (index <= 2)
        {
            CardPositioned += (receivedCard) => 
            {
                if (receivedCard == card)
                {
                    RotateAndFlipCard(index, card);
                }
            };
        }
        else
        {
            card.FaceDown();
        }
    }

    private void RotateAndFlipCard(int index, Card card)
    {
        card.FaceUp();
        
        card.PivotOffset = new Vector2(0, card.Size.Y / 2);

        float degree = (index % 2 == 0 ? -1 : 1) * 75;
        
        var tween = GetTree().CreateTween();
        tween.TweenProperty(card, "rotation_degrees", degree, 0.3)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }
}
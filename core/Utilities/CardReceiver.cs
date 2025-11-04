namespace Schnopsn.core.Utilities;

using Godot;
using Schnopsn.components.card;
using System;

public partial class CardReceiver : Node2D
{
    [Signal]
    public delegate void CardPositionedEventHandler(Card card);

    public virtual void ReceiveCard(Card card)
    {
        TransferOwnership(card);
        AnimateCardToPilePosition(card);
    }

    private void TransferOwnership(Card card)
    {
        var cardInitialPosition = card.GlobalPosition;

        card.GetParent()?.RemoveChild(card);
        AddChild(card);

        card.GlobalPosition = cardInitialPosition;
        card.Play();
    }

    protected virtual Vector2 GetTargetPosition(Card card)
    {
        return GlobalPosition;
    }

    private async void AnimateCardToPilePosition(Card card)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        var targetPosition = GetTargetPosition(card);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(card, "global_position", targetPosition, 0.4)
             .SetTrans(Tween.TransitionType.Quint)
             .SetEase(Tween.EaseType.Out);

        await ToSignal(tween, Tween.SignalName.Finished);
        EmitSignal(SignalName.CardPositioned, card);
    }
}
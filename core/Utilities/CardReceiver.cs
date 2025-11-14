namespace Schnopsn.core.Utilities;

using Godot;
using Schnopsn.components.card;

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

    private void AnimateCardToPilePosition(Card card)
    {
        // Wenn wir nicht mehr im Tree sind, gar nicht erst loslegen
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
            return;

        if (!GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return;

        // Auf nächsten Frame verschieben
        CallDeferred(MethodName.StartTween, card);
    }

    private void StartTween(Card card)
    {
        // Szene evtl. schon gewechselt → nochmal prüfen
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
            return;

        if (!GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return;

        var tree = GetTree();
        if (tree == null)
            return;

        var targetPosition = GetTargetPosition(card);

        var tween = tree.CreateTween();
        if (tween == null)
            return;

        tween.TweenProperty(card, "global_position", targetPosition, 0.4f)
            .SetTrans(Tween.TransitionType.Quint)
            .SetEase(Tween.EaseType.Out);

        tween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(this) && IsInsideTree())
            {
                EmitSignal(SignalName.CardPositioned, card);
            }
        };
    }


}
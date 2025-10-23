namespace Schnopsn.components.play_area;

using Godot;
using Schnopsn.components.card;


public partial class PlayArea : Node2D
{
    public async void ReceiveCard(Card card)
    {
        var startPosition = card.GlobalPosition;

        card.GetParent().RemoveChild(card);
        AddChild(card);

        card.GlobalPosition = startPosition;
        card.Play();

        Vector2 targetPosition = GlobalPosition;

        var tween = GetTree().CreateTween();
        tween.TweenProperty(card, "global_position", targetPosition, 0.4)
             .SetTrans(Tween.TransitionType.Quint)
             .SetEase(Tween.EaseType.Out);

        await ToSignal(tween, Tween.SignalName.Finished);

        // TODO: Set card to CardState.Played and handle accordingly.
    }
}

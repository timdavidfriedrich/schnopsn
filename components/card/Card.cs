namespace Schnopsn.components.card;

using Godot;


public partial class Card : TextureRect
{
    public CardColor Color { get; private set; }
    public CardValue Value { get; private set; }

    [Signal]
    public delegate void ClickedEventHandler(Card card);

    public Control Placeholder { get; set; }
    public CardState State { get; private set; } = CardState.InHand;

    private Vector2 _originalPosition;
    private Vector2 _originalScale;

    private const float _followSpeed = 15.0f;
    private const double _duration = 0.15;

    public Card WithData(CardColor color, CardValue value)
    {
        Color = color;
        Value = value;

        var path = $"res://components/card/assets/{color}_{value}.png";
        Texture = GD.Load<Texture2D>(path);
        return this;
    }

    public override void _Ready()
    {
        _originalPosition = Position;
        _originalScale = Scale;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        AnimatePosition(delta);
    }

    private void AnimatePosition(double delta)
    {
        if (IsInstanceValid(Placeholder) && (State == CardState.InHand || State == CardState.Selected))
        {
            GlobalPosition = GlobalPosition.Lerp(Placeholder.GlobalPosition, (float)delta * _followSpeed);
        }
        else if (State == CardState.Transitioning)
        {
            // Do nothing, the card is being played and should not follow anything
        }
        else
        {
            Position = Position.Lerp(_originalPosition, (float)delta * _followSpeed);
            Scale = Scale.Lerp(_originalScale, (float)delta * _followSpeed);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (State != CardState.InHand && State != CardState.Selected) return;

        bool isTap = @event is InputEventScreenTouch touchEvent && touchEvent.Pressed;

        if (isTap)
        {
            GetViewport().SetInputAsHandled();
            EmitSignal(SignalName.Clicked, this);
        }
    }

    public void Select()
    {
        if (State != CardState.InHand) return;
        State = CardState.Selected;

        Tween tween = GetTree().CreateTween();
        tween.SetParallel(true);
        Vector2 scaleIfSelected = _originalScale * new Vector2(1.25f, 1.25f);
        float positionFactor = GlobalPosition.Y < GetViewportRect().Size.Y / 2 ? 1 : -1;
        float distance = 10f;
        tween.TweenProperty(this, "position:y", positionFactor * distance, _duration);
        tween.TweenProperty(this, "scale", scaleIfSelected, _duration);
    }

    public void Deselect()
    {
        if (State != CardState.Selected) return;
        State = CardState.InHand;

        var tween = GetTree().CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "position:y", _originalPosition.Y, _duration).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "position:x", _originalPosition.X, _duration).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "scale", _originalScale, _duration).SetTrans(Tween.TransitionType.Quad);
    }

    public void Play()
    {
        State = CardState.Transitioning;
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "scale", _originalScale, _duration).SetTrans(Tween.TransitionType.Quad);
        Placeholder = null;
    }
}


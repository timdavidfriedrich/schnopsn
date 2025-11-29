namespace Schnopsn.components.card;

using Godot;
using System.Drawing;
using System.Threading.Tasks;


public partial class Card : TextureRect
{
    public CardColor Color { get; private set; }
    public CardValue Value { get; private set; }

    public bool IsFaceUp { get; private set; }

    public bool isPlayerCard { get; set; }

    [Signal]
    public delegate void ClickedEventHandler(Card card);

    [Export]
    private TextureRect _shadow;

    public Control Placeholder { get; set; }
    public CardState State { get; set; } = CardState.Idle;

    private Texture2D _backTexture;
    private Texture2D _frontTexture;

    private Vector2 _originalPosition;
    private Vector2 _originalScale;

    private const float _followSpeed = 15.0f;
    private const double _duration = 0.15;
    private const float _selectedScaleMultiplier = 1.25f;
    private const float _selectedPositionOffset = 10.0f;
	private const float _maxRotationDegrees = 12f;

    private Vector2 _viewportCenter;

    private float _currentVerticalOffset = 1.0f;
    private float _currentMaxHorizontalOffset = 1.0f;
    private Tween _shadowTween;

    private int _originalZIndex;
    private int _originalShadowZIndex;
    private int _zIndexOffset = 69;
    private int _shadowZIndexOffset = 42;

    private RandomNumberGenerator _random = new();


    public Card WithData(CardColor color, CardValue value)
    {
        Color = color;
        Value = value;

        _backTexture = GD.Load<Texture2D>("res://components/card/assets/rueckseite.png");
        _frontTexture = GD.Load<Texture2D>($"res://components/card/assets/{color}_{value}.png");
        Texture = _backTexture;
        IsFaceUp = false;
        return this;
    }

    public override void _Ready()
    {
        _random.Randomize();
        _viewportCenter = GetViewportRect().Size / 2.0f;
        _originalPosition = Position;
        _originalScale = Scale;
        MouseFilter = MouseFilterEnum.Stop;
        _originalZIndex = ZIndex;
        _originalShadowZIndex = _shadow.ZIndex;
    }

    public override void _Process(double delta)
    {
        HandleShadow();
    }

    public async void FaceUp()
    {
        if (IsFaceUp) return;
        IsFaceUp = true;
        await FlipAnimation(_frontTexture);
    }

    public async void FaceDown()
    {
        if (!IsFaceUp) return;
        IsFaceUp = false;
        await FlipAnimation(_backTexture);
    }

    private async Task FlipAnimation(Texture2D newTexture)
    {
        var tween = GetTree().CreateTween();
        tween.SetEase(Tween.EaseType.InOut);
        tween.SetTrans(Tween.TransitionType.Quad);

        tween.TweenProperty(this, "scale:x", 0.0f, 0.15);
        await ToSignal(tween, Tween.SignalName.Finished);

        Texture = newTexture;

        tween = GetTree().CreateTween();
        tween.SetEase(Tween.EaseType.InOut);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "scale:x", _originalScale.X, 0.15);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void HandleShadow()
    {
        Vector2 cardCenter = GlobalPosition + (Size / 2f);
        float distanceToViewportCenter = cardCenter.X - _viewportCenter.X;

        float targetMaxHorizontalOffset = State switch
        {
            CardState.Selected => 10.0f,
            CardState.Transitioning => 10.0f,
            _ => 1.0f
        };
        
        float targetVerticalOffset = State switch
        {
            CardState.Selected => 10.0f,
            CardState.Transitioning => 10.0f,
            _ => 1.0f
        };

        bool needsUpdate = !Mathf.IsEqualApprox(_currentMaxHorizontalOffset, targetMaxHorizontalOffset) || 
                           !Mathf.IsEqualApprox(_currentVerticalOffset, targetVerticalOffset);
        
        if (needsUpdate && (_shadowTween == null || !_shadowTween.IsRunning()))
        {
            _shadowTween?.Kill();
            _shadowTween = GetTree().CreateTween();
            _shadowTween.SetParallel(true);
            _shadowTween.TweenProperty(this, "_currentMaxHorizontalOffset", targetMaxHorizontalOffset, _duration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            _shadowTween.TweenProperty(this, "_currentVerticalOffset", targetVerticalOffset, _duration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
        }
        
        float horizontalOffset = Mathf.Lerp(
            0.0f, 
            -Mathf.Sign(distanceToViewportCenter) * _currentMaxHorizontalOffset, 
            Mathf.Abs(distanceToViewportCenter / _viewportCenter.X)
        );
        
        float rotationRad = Rotation;
        Vector2 localOffset = new(
            horizontalOffset * Mathf.Cos(-rotationRad) - _currentVerticalOffset * Mathf.Sin(-rotationRad),
            horizontalOffset * Mathf.Sin(-rotationRad) + _currentVerticalOffset * Mathf.Cos(-rotationRad)
        );
        
        _shadow.Position = localOffset;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (State != CardState.Idle && State != CardState.Selected) return;

        bool isTap = @event is InputEventScreenTouch touchEvent && touchEvent.Pressed;

        if (isTap)
        {
            GetViewport().SetInputAsHandled();
            EmitSignal(SignalName.Clicked, this);
        }
    }

    public void Select()
    {
        if (State != CardState.Idle) return;
        State = CardState.Selected;

        ZIndex = _zIndexOffset;
        _shadow.ZIndex =  _shadowZIndexOffset;

        Tween tween = GetTree().CreateTween();
        tween.SetParallel(true);
        Vector2 scaleIfSelected = _originalScale * _selectedScaleMultiplier;
        tween.TweenProperty(this, "position:y", -1 * _selectedPositionOffset, _duration);
        tween.TweenProperty(this, "scale", scaleIfSelected, _duration);
    }

    public void Deselect()
    {
        if (State != CardState.Selected) return;
        State = CardState.Idle;

        ZIndex = _originalZIndex;
        _shadow.ZIndex = _originalShadowZIndex;

        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "scale", _originalScale, _duration).SetTrans(Tween.TransitionType.Quad);
    }

    public void Play()
    {
        State = CardState.Transitioning;

        ZIndex = _originalZIndex;
        _shadow.ZIndex = _originalShadowZIndex;

        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "scale", _originalScale, _duration).SetTrans(Tween.TransitionType.Quad);
        Placeholder = null;
    }

    public void Add90DegreeRotation()
	{
    	float minus90Degrees = -90f;
		Vector2 pivotOffset = new(
			Size.X * 0.75f,
			Size.Y / 2f - Size.X / 4f
		);
		AddRotation(minus90Degrees, pivotOffset);
	}

    public void AddRandomRotation()
    {
		float randomDegrees = _random.Randf() * _maxRotationDegrees - (_maxRotationDegrees / 2f);
		Vector2 cardCenter = new(Size.X / 2, Size.Y / 2);
		AddRotation(randomDegrees, cardCenter);
    }

	private void AddRotation(float rotation, Vector2? pivotOffset = null)
	{
		PivotOffset = pivotOffset ?? new Vector2(Size.X / 2, Size.Y / 2);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(this, "rotation_degrees", rotation, 0.3)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}
}


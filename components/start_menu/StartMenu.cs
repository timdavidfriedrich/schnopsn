namespace Schnopsn.components.start_menu;

using Godot;


public partial class StartMenu : Panel
{
    [Export]
    private StartMenuCard _startMenuCard;

    [Export]
    private PackedScene _gameScene;

    [Export]
    private RichTextLabel _tapToStartLabel;
    private Color _originalLabelColor;
    private Tween _colorTween;

    private bool _isReadyToStart = false;
    private bool _isTransitioning = false;

    public override void _Ready()
    {
        _originalLabelColor = _tapToStartLabel.Modulate;
        _tapToStartLabel.Modulate = Colors.Transparent;
        _startMenuCard.ReadyToStart += OnReadyToStart;
    }

    public override void _ExitTree()
    {
        _startMenuCard.ReadyToStart -= OnReadyToStart;
    }

    private void OnReadyToStart()
    {
        _isReadyToStart = true;
        StartLabelAnimation();
    }

    private async void StartLabelAnimation()
    {
        Color pulseColor = _originalLabelColor.Lightened(0.3f);
        while (true)
        {
            _colorTween?.Kill();
            _colorTween = GetTree().CreateTween();
            _colorTween.TweenProperty(_tapToStartLabel, "modulate", pulseColor, 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            await ToSignal(_colorTween, "finished");
            _colorTween?.Kill();
            _colorTween = GetTree().CreateTween();
            _colorTween.TweenProperty(_tapToStartLabel, "modulate", _originalLabelColor, 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            await ToSignal(_colorTween, "finished");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isReadyToStart || _isTransitioning) return;
        bool isTap = @event is InputEventScreenTouch touchEvent && touchEvent.Pressed;
        if (!isTap) return;
        
        _isTransitioning = true;
        TransitionToGame();
        AcceptEvent();
    }

    private async void TransitionToGame()
    {
        var viewport = GetViewport();
        var viewportSize = viewport.GetVisibleRect().Size;
        
        var gameScene = _gameScene.Instantiate<Node>();
        GetTree().Root.AddChild(gameScene);
        
        if (gameScene is Control gameControl)
        {
            gameControl.Position = new Vector2(0, -viewportSize.Y);
        }
        
        var tween = GetTree().CreateTween();
        tween.SetParallel(true);
        
        tween.TweenProperty(this, "position", new Vector2(0, viewportSize.Y), 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        
        if (gameScene is Control gc)
        {
            tween.TweenProperty(gc, "position", Vector2.Zero, 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
        }
        
        await ToSignal(tween, Tween.SignalName.Finished);
        
        QueueFree();
    }
}

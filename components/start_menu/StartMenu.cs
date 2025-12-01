namespace Schnopsn.components.start_menu;

using Godot;
using Schnopsn.components.difficulty;
using Schnopsn.core;

public partial class StartMenu : Panel
{
    [Export]
    private StartMenuCard _startMenuCard;

    [Export]
    private PackedScene _gameScene;

    private Tween _colorTween;

    [Export]
    private StartButton _startButton;

    [Export]
	internal DifficultyDisplay _difficultyDisplay;

    internal DifficultyManager _difficultyManager;


    private bool _isReadyToStart = false;
    private bool _isTransitioning = false;

    public override void _Ready()
    {
        _startButton.Modulate = new Color(1, 1, 1, 0);
        _difficultyDisplay.Modulate = new Color(1, 1, 1, 0);
        _startMenuCard.ReadyToStart += OnReadyToStart;
        _startButton.StartButtonClicked += OnStartButtonClicked;
        _difficultyDisplay.DifficultyChanged += OnDifficultyChanged;

        InitDifficultyFromLastRound();
    }

    public override void _ExitTree()
    {
        _startMenuCard.ReadyToStart -= OnReadyToStart;
        _startButton.StartButtonClicked -= OnStartButtonClicked;
		_difficultyDisplay.DifficultyChanged -= OnDifficultyChanged;
    }

    private void OnReadyToStart()
    {
        _isReadyToStart = true;
        StartButtonFadeInAnimation();
        DifficultyDisplayFadeInAnimation();
    }

    private void OnDifficultyChanged()
	{
		_difficultyManager.ToggleDifficulty();
		_difficultyDisplay.SetDifficultyLevel(_difficultyManager.EnemyDifficulty);
		GD.Print($"Enemy difficulty changed to {_difficultyManager.EnemyDifficulty}");
	}

    private void InitDifficultyFromLastRound()
	{
		_difficultyManager = DifficultyManager.Instance;
		if (_difficultyManager == null)
		{
			GD.PrintErr("DifficultyManager instance not found!");
			return;
		}
		_difficultyDisplay.SetDifficultyLevel(_difficultyManager.EnemyDifficulty);
	}

    private async void StartButtonFadeInAnimation()
    {
        _startButton.Modulate = new Color(1, 1, 1, 0);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_startButton, "modulate:a", 1f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async void DifficultyDisplayFadeInAnimation()
    {
        _difficultyDisplay.Modulate = new Color(1, 1, 1, 0);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_difficultyDisplay, "modulate:a", 1f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void OnStartButtonClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        TransitionToGame();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMGoBackRequest)
        {
            GetViewport()?.SetInputAsHandled();
            GetTree()?.Quit();
        }
    }

    private async void TransitionToGame()
    {
        var viewport = GetViewport();
        var viewportSize = viewport.GetVisibleRect().Size;
        
        var coverPanel = new ColorRect();
        coverPanel.Color = new Color(0.14901961f, 0.36078432f, 0.25882354f, 1f);
        
        coverPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        coverPanel.Size = viewportSize;
        coverPanel.ZIndex = -1;
        
        GetTree().Root.AddChild(coverPanel);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        var tween = GetTree().CreateTween();               
        tween.TweenProperty(this, "global_position", new Vector2(0, viewportSize.Y), 0.75f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        
        await ToSignal(tween, Tween.SignalName.Finished);
        
        GD.Print(">>>> Transition finished");
        _isTransitioning = false;
        
        GetTree().ChangeSceneToPacked(_gameScene);
        coverPanel.QueueFree();
    }
}

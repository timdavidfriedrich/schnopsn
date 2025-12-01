namespace Schnopsn.components.difficulty;

using Godot;
using Schnopsn.core;

public partial class DifficultyDisplay : Control
{
    [Signal]
    public delegate void DifficultyChangedEventHandler();

    [Export]
    private AnimationPlayer _animationPlayer;
    private int _difficultyLevel = 1;

    public override void _GuiInput(InputEvent @event)
    {
        bool isTap = @event is InputEventScreenTouch eventScreenTouch && eventScreenTouch.Pressed;
        if (!isTap) return;
        EmitSignal(SignalName.DifficultyChanged);
        GetViewport()?.SetInputAsHandled();
    }

    public void SetDifficultyLevel(Difficulty difficulty)
    {
        _difficultyLevel = difficulty switch
        {
            Difficulty.Easy => 0,
            Difficulty.Medium => 1,
            Difficulty.Hard => 2,
            _ => 1
        };
        PlayDifficultyAnimation();
    }

    private void PlayDifficultyAnimation()
    {
        string animationName = _difficultyLevel switch
        {
            0 => "hard_to_easy",
            1 => "easy_to_middle",
            2 => "middle_to_hard",
            _ => "easy_to_middle"
        };
        _animationPlayer.Play(animationName);
    }
}

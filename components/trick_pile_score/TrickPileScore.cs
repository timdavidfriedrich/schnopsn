using Godot;
using System;

public partial class TrickPileScore : PanelContainer
{
    [Export]
    private RichTextLabel _scoreLabel;

    private bool _isVisible = false;

    public override void _Ready()
    {
        ResetScore();
    }

    public void ResetScore()
    {
        SetScore(0);
    }
    public void SetScore(int score)
    {
        if (score <= 0)
        {
            SetLabelVisible(false);
        }
        else if (!_isVisible)
        {
            SetLabelVisible(true);
        }
        _scoreLabel.Text = score.ToString();
    }

    private void SetLabelVisible(bool visible)
    {
        Visible = visible;
        _isVisible = visible;
    }
}

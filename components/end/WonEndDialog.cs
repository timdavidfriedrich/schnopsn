namespace Schnopsn.components.end;

using Godot;
using Schnopsn.core;

public partial class WonEndDialog : Panel
{
    // * Seems weird that there are 2 basically identical classes for won/lost end dialogs.
    // * However, there was some weird issue and I do not want to risk anything anymore.
    public override void _Input(InputEvent @event)
    {
        bool isTap = @event is InputEventScreenTouch eventScreenTouch && eventScreenTouch.Pressed;
        if (!isTap) return;
        BummerlManager.Instance.ResetAllBummerl();
        GetViewport()?.SetInputAsHandled();
        GetTree()?.ChangeSceneToFile("res://components/start_menu/StartMenu.tscn");
    }
}

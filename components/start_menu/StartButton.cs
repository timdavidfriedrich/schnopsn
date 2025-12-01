namespace Schnopsn.components.start_menu;

using Godot;


public partial class StartButton : Control
{
    [Signal]
    public delegate void StartButtonClickedEventHandler();

    [Export]
    private TextureButton _button;

    public override void _Ready()
    {
        _button.Pressed += OnStartButtonReleased;
    }

    public override void _ExitTree()
    {
        _button.Pressed -= OnStartButtonReleased;
    }

    private void OnStartButtonReleased()
    {
        EmitSignal(SignalName.StartButtonClicked);
    }
}

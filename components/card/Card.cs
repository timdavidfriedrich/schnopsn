using Godot;
using System;

public enum CardState {
    Idle,
    Selected,
    Dragged
}

public partial class Card : Node2D {

    private Vector2 initialScale;

    public CardState State { get; set; } = CardState.Idle;

    public override void _Ready() {
        initialScale = Scale;
        UpdateVisualState();
    }

    public override void _Process(double delta) {
        UpdateVisualState();
    }

    private void UpdateVisualState() {
        switch (State) {
            case CardState.Idle:
                Scale = initialScale;
                ZIndex = 0;
                break;
            case CardState.Selected:
                Scale = initialScale * 1.2f;
                ZIndex = 1;
                break;
            case CardState.Dragged:
                Scale = initialScale * 1.4f;
                ZIndex = 2;
                break;
        }
    }

}

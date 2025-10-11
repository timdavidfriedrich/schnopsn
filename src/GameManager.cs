using Godot;
using System;

public partial class GameManager : Node2D
{

    public override void _Ready()
    {
        var cardSpawner = GetNode<Node2D>("CardSpawner");
        var cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
        for (int i = 0; i < 20; i++)
        {
            Node2D card = (Node2D) cardScene.Instantiate();
            AddChild(card);
            card.GlobalPosition = cardSpawner.GlobalPosition;
        }
    }

    public override void _Process(double delta)
    {
        
    }
}

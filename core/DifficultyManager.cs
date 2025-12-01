namespace Schnopsn.core;

using Godot;


public partial class DifficultyManager : Node
{
    public static DifficultyManager Instance { get; private set; }
    public Difficulty EnemyDifficulty { get; private set; } = Difficulty.Medium;

    public void ResetDifficulty()
    {
        EnemyDifficulty = Difficulty.Medium;
    }

    public void ToggleDifficulty()
    {
        EnemyDifficulty = EnemyDifficulty switch
        {
            Difficulty.Easy => Difficulty.Medium,
            Difficulty.Medium => Difficulty.Hard,
            Difficulty.Hard => Difficulty.Easy,
            _ => Difficulty.Medium
        };
    }

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree(); 
        }
    }
}
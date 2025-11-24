namespace Schnopsn.core;


using Godot;
using Schnopsn.core.Utilities;
using System;

public partial class BummerlManager : Node
{
    public static BummerlManager Instance { get; private set; }
    public int PlayerBummerl { get; private set; } = Rules.INITIAL_BUMMERL;
    public int EnemyBummerl { get; private set; } = Rules.INITIAL_BUMMERL;

    public void ResetAllBummerl()
    {
        PlayerBummerl = Rules.INITIAL_BUMMERL;
        EnemyBummerl = Rules.INITIAL_BUMMERL;
    }

    public void ReducePlayerBummerl(int bummerl)
    {
        PlayerBummerl = Math.Max(0, PlayerBummerl - bummerl);
    }

    public void ReduceEnemyBummerl(int bummerl)
    {
        EnemyBummerl = Math.Max(0, EnemyBummerl - bummerl);
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
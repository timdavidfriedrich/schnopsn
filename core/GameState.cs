using System.Collections.Generic;
using System.Linq;
using Schnopsn.components.card;
using Schnopsn.core.Utilities;

public class GameState
{
    public List<CardData> PlayerHand { get; set; }
    public List<CardData> EnemyHand { get; set; }
    public List<CardData> PlayedCards { get; set; } = new();
    public int PlayerPoints { get; set; }
    public int EnemyPoints { get; set; }
    public CardColor TrumpColor { get; set; }
    public bool TalonClosed { get; set; }

    public PlayerRole CurrentPlayer { get; set; }

    public GameState Clone()
    {
        return new GameState
        {
            PlayerHand = new List<CardData>(PlayerHand),
            EnemyHand = new List<CardData>(EnemyHand),
            PlayedCards = new List<CardData>(PlayedCards),
            PlayerPoints = PlayerPoints,
            EnemyPoints = EnemyPoints,
            TrumpColor = TrumpColor,
            TalonClosed = TalonClosed,
            CurrentPlayer = CurrentPlayer
        };
    }

    public void ApplyMove(PlayerRole player, CardData card)
    {
        if (player == PlayerRole.Player)
        {
            PlayerHand.Remove(card);
            PlayedCards.Add(card);
            PlayerPoints += Rules.Points(card.Value);
            CurrentPlayer = PlayerRole.Enemy;
        }
        else
        {
            EnemyHand.Remove(card);
            PlayedCards.Add(card);
            EnemyPoints += Rules.Points(card.Value);
            CurrentPlayer = PlayerRole.Player;
        }
    }

    public List<CardData> GetValidMoves(PlayerRole player)
    {
        var hand = player == PlayerRole.Player ? PlayerHand : EnemyHand;
        return hand.ToList();
    }

    public bool IsTerminal()
    {
        return PlayerPoints >= 66
            || EnemyPoints >= 66
            || PlayerHand.Count == 0
            || EnemyHand.Count == 0;
    }

    public int GetPlayerPoints() => PlayerPoints;
    public int GetEnemyPoints() => EnemyPoints;

    public PlayerRole Player => PlayerRole.Player;
    public PlayerRole Enemy => PlayerRole.Enemy;
}

public enum PlayerRole
{
    Player,
    Enemy
}

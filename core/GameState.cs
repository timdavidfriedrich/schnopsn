using System;
using System.Collections.Generic;
using System.Linq;
using Schnopsn.components.card;
using Schnopsn.components.hand;
using Schnopsn.core;
using Schnopsn.core.Utilities;

public class GameState
{
    public List<Card> PlayerHand { get; set; }
    public List<Card> EnemyHand { get; set; }
    public List<Card> PlayedCards { get; set; } = new();
    public int PlayerPoints { get; set; }
    public int EnemyPoints { get; set; }
    public CardColor TrumpColor { get; set; }
    public bool TalonClosed { get; set; }

    public PlayerRole CurrentPlayer { get; set; } // enum { Player, Enemy }

    public static GameState FromCurrent(Game game)
    {
        return new GameState
        {
            PlayerHand = game._playerHand.CardsInHand.Select(CloneCard).ToList(),
            EnemyHand = game._enemyHand.CardsInHand.Select(CloneCard).ToList(),
            PlayerPoints = game._playerScore + game._playerExtraPoints,
            EnemyPoints = game._enemyScore + game._enemyExtraPoints,
            TrumpColor = game.trumpColor,
            TalonClosed = game._isTalonClosed,
            CurrentPlayer = PlayerRole.Enemy // weil Enemy KI ausführt
        };
    }

    public GameState Clone()
    {
        return new GameState
        {
            PlayerHand = PlayerHand.Select(CloneCard).ToList(),
            EnemyHand = EnemyHand.Select(CloneCard).ToList(),
            PlayedCards = PlayedCards.Select(CloneCard).ToList(),
            PlayerPoints = PlayerPoints,
            EnemyPoints = EnemyPoints,
            TrumpColor = TrumpColor,
            TalonClosed = TalonClosed,
            CurrentPlayer = CurrentPlayer
        };
    }

    public void ApplyMove(PlayerRole player, Card card)
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

    public List<Card> GetValidMoves(PlayerRole player)
    {
        var hand = player == PlayerRole.Player ? PlayerHand : EnemyHand;
        return hand.ToList(); // Vereinfachung: alle Karten erlaubt
    }

    public bool IsTerminal()
    {
        // Ende wenn:
        // - jemand 66+ Punkte hat ODER
        // - eine der beiden Hände leer ist
        return PlayerPoints >= 66
            || EnemyPoints >= 66
            || PlayerHand.Count == 0
            || EnemyHand.Count == 0;
    }
    
    public int GetPlayerPoints() => PlayerPoints;
    public int GetEnemyPoints() => EnemyPoints;

    private static Card CloneCard(Card original)
    {
        var clone = new Card().WithData(original.Color, original.Value);
        clone.State = original.State;
        return clone;
    }

    public PlayerRole Player => PlayerRole.Player;
    public PlayerRole Enemy => PlayerRole.Enemy;
}

public enum PlayerRole
{
    Player,
    Enemy
}

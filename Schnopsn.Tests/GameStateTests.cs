using System.Collections.Generic;
using Schnopsn.components.card;
using Xunit;

namespace Schnopsn.Tests;

public class GameStateTests
{
    private static GameState MakeState(
        List<CardData>? playerHand = null,
        List<CardData>? enemyHand = null,
        int playerPoints = 0,
        int enemyPoints = 0,
        PlayerRole currentPlayer = PlayerRole.Player)
    {
        return new GameState
        {
            PlayerHand = playerHand ?? [new CardData(CardColor.herz, CardValue.sau)],
            EnemyHand  = enemyHand  ?? [new CardData(CardColor.schellen, CardValue.zehn)],
            PlayerPoints = playerPoints,
            EnemyPoints  = enemyPoints,
            TrumpColor   = CardColor.eichel,
            CurrentPlayer = currentPlayer
        };
    }

    // --- ApplyMove ---

    [Fact]
    public void ApplyMove_Player_AddsPointsAndRemovesCard()
    {
        var card  = new CardData(CardColor.herz, CardValue.sau);
        var state = MakeState(playerHand: [card]);

        state.ApplyMove(PlayerRole.Player, card);

        Assert.Equal(11, state.PlayerPoints);
        Assert.DoesNotContain(card, state.PlayerHand);
    }

    [Fact]
    public void ApplyMove_Enemy_AddsPointsAndRemovesCard()
    {
        var card  = new CardData(CardColor.schellen, CardValue.zehn);
        var state = MakeState(enemyHand: [card]);

        state.ApplyMove(PlayerRole.Enemy, card);

        Assert.Equal(10, state.EnemyPoints);
        Assert.DoesNotContain(card, state.EnemyHand);
    }

    [Fact]
    public void ApplyMove_PlayerTurn_CurrentPlayerBecomesEnemy()
    {
        var card  = new CardData(CardColor.herz, CardValue.sau);
        var state = MakeState(playerHand: [card], currentPlayer: PlayerRole.Player);

        state.ApplyMove(PlayerRole.Player, card);

        Assert.Equal(PlayerRole.Enemy, state.CurrentPlayer);
    }

    [Fact]
    public void ApplyMove_EnemyTurn_CurrentPlayerBecomesPlayer()
    {
        var card  = new CardData(CardColor.schellen, CardValue.zehn);
        var state = MakeState(enemyHand: [card], currentPlayer: PlayerRole.Enemy);

        state.ApplyMove(PlayerRole.Enemy, card);

        Assert.Equal(PlayerRole.Player, state.CurrentPlayer);
    }

    // --- IsTerminal ---

    [Fact]
    public void IsTerminal_PlayerAt66_True()
    {
        var state = MakeState(playerPoints: 66);
        Assert.True(state.IsTerminal());
    }

    [Fact]
    public void IsTerminal_EnemyAt66_True()
    {
        var state = MakeState(enemyPoints: 66);
        Assert.True(state.IsTerminal());
    }

    [Fact]
    public void IsTerminal_BothBelow66WithCards_False()
    {
        var state = MakeState(playerPoints: 30, enemyPoints: 40);
        Assert.False(state.IsTerminal());
    }

    [Fact]
    public void IsTerminal_EmptyPlayerHand_True()
    {
        var state = MakeState(playerHand: []);
        Assert.True(state.IsTerminal());
    }

    // --- Clone ---

    [Fact]
    public void Clone_DeepCopy_OriginalUnchanged()
    {
        var card  = new CardData(CardColor.herz, CardValue.koenig);
        var state = MakeState(playerHand: [card], playerPoints: 10);

        var clone = state.Clone();
        clone.PlayerPoints = 99;
        clone.PlayerHand.Add(new CardData(CardColor.pik, CardValue.ober));

        Assert.Equal(10, state.PlayerPoints);
        Assert.Single(state.PlayerHand);
    }

    // --- GetValidMoves ---

    [Fact]
    public void GetValidMoves_ReturnsAllCardsForPlayer()
    {
        var hand  = new List<CardData>
        {
            new(CardColor.herz,     CardValue.sau),
            new(CardColor.schellen, CardValue.zehn),
            new(CardColor.eichel,   CardValue.koenig)
        };
        var state = MakeState(playerHand: hand);

        var moves = state.GetValidMoves(PlayerRole.Player);

        Assert.Equal(hand.Count, moves.Count);
    }
}

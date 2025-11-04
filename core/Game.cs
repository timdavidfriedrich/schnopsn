namespace Schnopsn.core;

using Godot;
using Schnopsn.components.card;
using Schnopsn.components.hand;
using Schnopsn.components.play_area;
using Schnopsn.components.draw_pile;
using Schnopsn.core.Utilities;
using System;
using System.Collections.Generic;
using Schnopsn.components.trick_pile;
using System.Threading.Tasks;

public partial class Game : Node2D
{
    [Export]
    private float _playAreaWaitingTimeMillis = 500f;
    [Export]
    private Hand _playerHand;
    [Export]
    private Hand _enemyHand;
    [Export]
    private TrickPile _playerTrickPile;
    [Export]
    private TrickPile _enemyTrickPile;
    [Export]
    private PlayArea _playArea;
    [Export]
    private DrawPile _drawPile;

    [Export]
    private PackedScene _cardScene;

    private Card[] _cards;

    public override async void _Ready()
    {
        SubscribeToSignals();

        CreateAndShuffleCards();
        await AddCardsToPile();
        DealCardsToHand(_enemyHand, 5);
        DealCardsToHand(_playerHand, 5);
    }

    public override void _ExitTree()
    {
        UnsubscribeFromSignals();
    }

    private void SubscribeToSignals()
    {
        _playerHand.WantsToPlayCard += OnHandWantsToPlayCard;
        _enemyHand.WantsToPlayCard += OnHandWantsToPlayCard;
        _playArea.BothCardsPlayed += OnBothCardsPlayed;
    }

    private void UnsubscribeFromSignals()
    {
        _playerHand.WantsToPlayCard -= OnHandWantsToPlayCard;
        _enemyHand.WantsToPlayCard -= OnHandWantsToPlayCard;
        _playArea.BothCardsPlayed -= OnBothCardsPlayed;
    }

    private void CreateAndShuffleCards()
    {
        List<(CardColor color, CardValue value)> _cardSpecs = [];
        foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
        {
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                _cardSpecs.Add((color, value));
            }
        }
        GD.Print($"Created {_cardSpecs.Count} cards.");

        _cardSpecs.Shuffle();

        var cards = new List<Card>(_cardSpecs.Count);
        foreach (var (color, value) in _cardSpecs)
        {
            var card = _cardScene.Instantiate<Card>();
            card = card.WithData(color, value);
            cards.Add(card);
        }

        _cards = cards.ToArray();
    }

    private async Task AddCardsToPile()
    {
        int cardsToPosition = _cards.Length;
        int cardsPositioned = 0;

        void OnCardPositioned(Card card)
        {
            cardsPositioned++;
        }

        _drawPile.CardPositioned += OnCardPositioned;

        foreach (Card card in _cards)
        {
            _drawPile.ReceiveCard(card);
        }
        GD.Print($"Added {_cards.Length} cards to draw pile.");

        while (cardsPositioned < cardsToPosition)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        _drawPile.CardPositioned -= OnCardPositioned;
        GD.Print("All cards positioned in draw pile.");

    }

    private void DealCardsToHand(Hand hand, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Card card = _drawPile.DrawCard();
            if (card != null)
            {
                hand.ReceiveCard(card);
            }
        }
        GD.Print($"Dealt {count} cards to {hand.Name}. DrawPile has {_drawPile.GetCardCount()} cards remaining.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
        {
            _playerHand.OnTouchOutside();
        }
    }

    private void OnHandWantsToPlayCard(Card card)
    {
        if (card.State != CardState.InHand && card.State != CardState.Selected)
        {
            GD.PrintErr("Attempted to play a card that is not in hand nor selected!");
            return;
        }
        _playArea.ReceiveCard(card);
    }

    private async void OnBothCardsPlayed(Card[] cards)
    {
        // TODO: Implement logic to determine winner
        // random trickPiles as winner for testing
        List<TrickPile> trickPiles = [_playerTrickPile, _enemyTrickPile];
        Random rnd = new();
        int winnerIndex = rnd.Next(trickPiles.Count);
        TrickPile winnerPile = trickPiles[winnerIndex];
        // ---

        await ToSignal(
            GetTree().CreateTimer(_playAreaWaitingTimeMillis / 1000f),
            Timer.SignalName.Timeout
        );

        foreach (Card card in cards)
        {
            winnerPile.ReceiveCard(card);
        }

        DealCardsToHand(_playerHand, 1);
        DealCardsToHand(_enemyHand, 1);
    }
}

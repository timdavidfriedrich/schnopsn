namespace Schnopsn.core;

using Godot;
using Schnopsn.components.card;
using Schnopsn.components.hand;
using Schnopsn.components.play_area;
using Schnopsn.components.draw_pile;
using Schnopsn.core.Utilities;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    [Export]
    private Hand _playerHand;
    [Export]
    private Hand _enemyHand;
    [Export]
    private PlayArea _playArea;
    [Export]
    private DrawPile _drawPile;
    [Export]
    private PackedScene _cardScene;

    // TODO: Remove this (das hatten wir ursprünglich zum Ausprobieren drin)
    // public override void _Ready()
    // {
    //     var cardSpawner = GetNode<Node2D>("CardSpawner");
    //     var cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
    //     for (int i = 0; i < 20; i++)
    //     {
    //         Node2D card = (Node2D) cardScene.Instantiate();
    //         AddChild(card);
    //         card.GlobalPosition = cardSpawner.GlobalPosition;
    //     }
    // }

    private Card[] _cards;

    public override void _Ready()
    {
        _playerHand.CardRequested += OnPlayerWantsToPlayCard;

        if (_cardScene == null)
        {
            GD.PrintErr("Card Scene is not set in the Game node inspector!");
            return;
        }

        if (_drawPile == null)
        {
            GD.PrintErr("DrawPile is not set in the Game node inspector!");
            return;
        }

        CreateAndShuffleCards();
        AddCardsToPile();
        DealCardsToHand(_enemyHand, 5);
        DealCardsToHand(_playerHand, 5);
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

    private void AddCardsToPile()
    {
        foreach (Card card in _cards)
        {
            _drawPile.AddCard(card);
        }
        GD.Print($"Added {_cards.Length} cards to draw pile.");
    }

    private void DealCardsToHand(Hand hand, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Card card = _drawPile.DrawCard();
            if (card != null)
            {
                hand.AddCard(card);
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

    private void OnPlayerWantsToPlayCard(Card card)
    {
        if (card.State != CardState.InHand && card.State != CardState.Selected)
        {
            GD.PrintErr("Attempted to play a card that is not in hand nor selected!");
            return;
        }
        _playArea.ReceiveCard(card);
    }
}

namespace Schnopsn.core;

using Godot;
using Schnopsn.components.card;
using Schnopsn.components.hand;
using Schnopsn.components.play_area;
using Schnopsn.components.draw_pile;
using Schnopsn.core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Schnopsn.components.trick_pile;

public partial class Game : Node2D
{
    [Export] private Hand _playerHand;
    [Export] private Hand _enemyHand;
    [Export] private TrickPile _playerTrickPile;
    [Export] private TrickPile _enemyTrickPile;
    [Export] private PlayArea _playArea;
    [Export] private DrawPile _drawPile;

    [Export] private PackedScene _cardScene;

    private Card[] _cards;

    private enum Side { Player, Enemy }
    private Side _leader = Side.Player;
    private Side _toMove = Side.Player;

    private int _playerPoints = 0;
    private int _enemyPoints = 0;

    private CardColor _trumpColor;
    private Card _trumpOpenCard;
    private Stack<Card> _talon = new();
    private bool _talonClosed = false;

    private Card _leadCard = null;
    private bool _isTrickInProgress = false;


    public override void _Ready()
    {
        SubscribeToSignals();

        CreateAndShuffleCards();
        AddCardsToPile();
        PrepareTalonAndHands();

        _leader = Side.Player;
        SetTurn(_leader);

        GD.Print("---- GAME START ----");
        GD.Print($"Trumpf ist: {_trumpColor}");
        GD.Print($"Player Hand: {DescribeHand(_playerHand)}");
        GD.Print($"Enemy  Hand: {DescribeHand(_enemyHand)}");
        GD.Print("---------------------");
    }

    public override void _ExitTree() => UnsubscribeFromSignals();

    private void SubscribeToSignals()
    {
        _playerHand.WantsToPlayCard += OnHandWantsToPlayCard;
        _playArea.BothCardsPlayed += OnBothCardsPlayed;
    }

    private void UnsubscribeFromSignals()
    {
        _playerHand.WantsToPlayCard -= OnHandWantsToPlayCard;
        _playArea.BothCardsPlayed -= OnBothCardsPlayed;
    }

    // ---------- Karten vorbereiten ----------

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
        _cardSpecs.Shuffle();

        var cards = new List<Card>(_cardSpecs.Count);
        foreach (var (color, value) in _cardSpecs)
        {
            var card = _cardScene.Instantiate<Card>().WithData(color, value);
            card.FaceDown();
            cards.Add(card);
        }

        _cards = cards.ToArray();
        GD.Print($"Created & shuffled {_cards.Length} cards.");
    }

    private void AddCardsToPile()
    {
        foreach (Card card in _cards) _drawPile.AddCard(card);
        GD.Print($"Added {_cards.Length} cards to draw pile.");
    }

    private void PrepareTalonAndHands()
    {
        var temp = new List<Card>();
        while (_drawPile.GetCardCount() > 0)
        {
            var c = _drawPile.DrawCard();
            if (c != null) temp.Add(c);
        }
        foreach (var c in temp) _talon.Push(c);

        DealFromTalon(_enemyHand, 3);
        DealFromTalon(_playerHand, 3);

        _trumpOpenCard = _talon.Pop();
        _trumpOpenCard.FaceUp();
        _trumpColor = _trumpOpenCard.Color;
        GD.Print($"[Setup] Trumpf ist {_trumpColor}");

        DealFromTalon(_enemyHand, 2);
        DealFromTalon(_playerHand, 2);
    }

    private void DealFromTalon(Hand hand, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_talon.Count == 0)
            {
                if (_trumpOpenCard != null)
                {
                    var c = _trumpOpenCard;
                    _trumpOpenCard = null;
                    c.FaceUp();
                    hand.AddCard(c);
                    GD.Print($"[Draw] {hand.Name} zieht offene Trumpfkarte {DescribeCard(c)}");
                }
                return;
            }

            var card = _talon.Pop();
            card.FaceUp();
            hand.AddCard(card);
            GD.Print($"[Draw] {hand.Name} zieht {DescribeCard(card)}");
        }
    }

    // ---------- Zugsteuerung ----------

    private void SetTurn(Side side)
    {
        _toMove = side;

        bool playerTurn = (_toMove == Side.Player);
        _playerHand.SetInteractive(playerTurn);
        if (!playerTurn) _playerHand.OnTouchOutside();

        GD.Print($"[TURN] {_toMove} ist am Zug.");

        if (_toMove == Side.Enemy)
            MaybeLetAiPlay();
    }


    private Side Other(Side s) => s == Side.Player ? Side.Enemy : Side.Player;

    // ---------- Eingabe & KI ----------

    private void OnHandWantsToPlayCard(Card card)
    {
        if (_toMove != Side.Player) { GD.Print("[Warn] Spieler nicht am Zug."); return; }
        if (card.State != CardState.InHand && card.State != CardState.Selected) { GD.PrintErr("Attempted to play a card not in hand!"); return; }

        bool playerIsLead = (_leadCard == null);

        PlayCardFromHand(_playerHand, card);
        if (playerIsLead)
        {
            _leadCard = card;                 // Spieler war Vorhand
            SetTurn(Side.Enemy);              // KI soll die ZWEITE Karte legen
        }
        else
        {
            // Spieler hat die ZWEITE Karte gelegt -> KEIN SetTurn hier!
            // OnBothCardsPlayed kümmert sich um Auswertung + nächste Vorhand
        }
    }


    private void MaybeLetAiPlay()
    {
        if (_toMove != Side.Enemy) return;

        // WICHTIG: nie spielen, wenn bereits 2 Karten im Stich liegen
        if (_playArea.GetCardCount() >= 2) return;

        bool aiIsLead = (_leadCard == null);

        var enemyCards = _enemyHand.GetCards();
        Card lead = _leadCard; // null wenn AI Vorhand
        var choice = SimpleAi.ChooseCardOpenTalon(enemyCards, _trumpColor, lead);

        GD.Print($"[AI] Enemy spielt {DescribeCard(choice)} (Trumpf: {_trumpColor})");

        PlayCardFromHand(_enemyHand, choice);

        if (aiIsLead)
        {
            // AI hat die ERSTE Karte gelegt -> Spieler ist dran für die ZWEITE
            _leadCard = choice;
            _toMove = Side.Player;
            _playerHand.SetInteractive(true);
            _playerHand.OnTouchOutside();
            GD.Print("[TURN] Player ist am Zug.");
        }
        else
        {
            // AI hat die ZWEITE Karte gelegt -> KEIN SetTurn hier!
            // OnBothCardsPlayed wird gleich feuern und den nächsten Stich starten
        }
    }


    private void PlayCardFromHand(Hand hand, Card card)
    {
        hand.RemoveCard(card);
        card.Play();
        _playArea.ReceiveCard(card);

        string who = (hand == _playerHand) ? "Player" : "Enemy";
        GD.Print($"[{who}] spielt {DescribeCard(card)}");

        // Markiere, dass gerade ein Stich läuft
        _isTrickInProgress = true;
    }


    // ---------- Stich-Auswertung ----------

    private void OnBothCardsPlayed(Card[] cards)
    {
        Card first = _leadCard;
        Card second = cards.First(c => c != first);

        Side firstSide = _leader;
        Side secondSide = Other(_leader);

        GD.Print($"[Stich] {firstSide} {DescribeCard(first)} vs {secondSide} {DescribeCard(second)}");

        Side winner = DetermineTrickWinnerOpen(first, second, firstSide, secondSide);
        int trickPoints = Rules.Points(first.Value) + Rules.Points(second.Value);

        if (winner == Side.Player) _playerPoints += trickPoints;
        else _enemyPoints += trickPoints;

        GD.Print($"[Result] {winner} gewinnt {trickPoints} Punkte (P/E: {_playerPoints}/{_enemyPoints})");

        TrickPile dest = winner == Side.Player ? _playerTrickPile : _enemyTrickPile;
        dest.ReceiveCard(first);
        dest.ReceiveCard(second);

        if (_playerPoints >= 66 || _enemyPoints >= 66)
        {
            var s = _playerPoints >= 66 ? "PLAYER" : "ENEMY";
            GD.Print($"🎉 Spielende: {s} erreicht 66 Punkte!");
            return;
        }

        if (!_talonClosed)
        {
            if (winner == Side.Player) { DealFromTalon(_playerHand, 1); } else { DealFromTalon(_enemyHand, 1); }
            if (winner == Side.Player) { DealFromTalon(_enemyHand, 1); } else { DealFromTalon(_playerHand, 1); }

            if (_talon.Count == 0 && _trumpOpenCard == null)
            {
                _talonClosed = true;
                GD.Print("[Info] Talon leer – geschlossenes Spiel (noch TODO)");
            }
        }

        _leader = winner;
        _leadCard = null;

        GD.Print($"-----------------------------");
        GD.Print($"[Next Trick] {_leader} spielt als Vorhand");
        GD.Print($"-----------------------------");

        _isTrickInProgress = false;

        SetTurn(_leader);
    }

    private Side DetermineTrickWinnerOpen(Card first, Card second, Side firstSide, Side secondSide)
    {
        bool firstIsTrump = (first.Color == _trumpColor);
        bool secondIsTrump = (second.Color == _trumpColor);

        if (firstIsTrump && secondIsTrump)
            return Rules.Rank(first.Value) >= Rules.Rank(second.Value) ? firstSide : secondSide;
        if (firstIsTrump && !secondIsTrump) return firstSide;
        if (!firstIsTrump && secondIsTrump) return secondSide;
        if (second.Color == first.Color)
            return Rules.Rank(first.Value) >= Rules.Rank(second.Value) ? firstSide : secondSide;
        return firstSide;
    }

    // ---------- Debug Helpers ----------

    private string DescribeCard(Card c) => $"{c.Color}-{c.Value}";
    private string DescribeHand(Hand h) => string.Join(", ", h.GetCards().Select(DescribeCard));

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
            _playerHand.OnTouchOutside();
    }
}

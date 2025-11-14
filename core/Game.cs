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
using System.Linq;


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

	private Card trumpCard;

	private CardColor trumpColor;

	private int _playerScore = 0;
	private int _enemyScore = 0;

	private int _playerExtraPoints = 0;
	private int _enemyExtraPoints = 0;

	public override async void _Ready()
	{
		SubscribeToSignals();

		CreateAndShuffleCards();
		await AddCardsToPile();

		DealCardsToHand(_playerHand, 3);

		DealCardsToHand(_enemyHand, 3);

		SetTrump();

		DealCardsToHand(_playerHand, 2);

		DealCardsToHand(_enemyHand, 2);
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

	private void SetTrump()
    {
		trumpCard = _drawPile.DrawCard();
		trumpColor = trumpCard.Color;
		_drawPile.ReceiveCard(trumpCard);
		GD.Print($"Trumpf ist {trumpColor} {trumpCard.Value}.");
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
				card.isPlayerCard = hand == _playerHand;
				hand.ReceiveCard(card);
			}
		}
		GD.Print($"Dealt {count} cards to {hand.Name}.");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
		{
			_playerHand.OnTouchOutside();
		}
	}

	private void OnHandWantsToPlayCard(Card card, Hand hand)
	{
		if (card.State != CardState.InHand && card.State != CardState.Selected)
		{
			GD.PrintErr("Attempted to play a card that is not in hand nor selected!");
			return;
		}

		// --- Trumpf-Unter-Tausch ---
		// Wenn der Spieler den Trumpf-Unter "spielt" und noch > 2 Karten im Talon sind,
		// darf er den Unter gegen die aufgedeckte Trumpfkarte tauschen.
		if (card.Color == trumpColor
			&& card.Value == CardValue.unter
			&& _drawPile.CardCount > 2              // richtiger Talon-Count
			&& _drawPile.ContainsCard(trumpCard))   // Trumpfkarte liegt noch im Talon
		{
			// Unter auf den Nachziehstapel legen (als neue offene Trumpfkarte)
			_drawPile.ReceiveCard(card);

			// Alte Trumpfkarte aus dem Talon entfernen (nur aus der internen Liste)
			_drawPile.RemoveCard(trumpCard);

			// Alte Trumpfkarte in die Hand des Spielers geben
			hand.ReceiveCard(trumpCard);

			// Neue "sichtbare" Trumpfkarte ist jetzt der Unter
			trumpCard = card;

			GD.Print($"{(hand == _playerHand ? "Player" : "Enemy")} performed Unter swap!");

			// WICHTIG:
			// Kein Ausspielen in die PlayArea – das war nur ein Tausch.
			// Der Spieler muss danach eine Karte normal spielen.
			return;
		}

		if (hand.CheckAnsage(card))
		{
			int extrapoints = 20;
			if (card.Color == trumpColor) extrapoints = 40;
			if (hand == _playerHand)
			{
				_playerExtraPoints += extrapoints;
				GD.Print($"Player announced {extrapoints} extra points!");
				//todo check game end
			}
			else
			{
				_enemyExtraPoints += extrapoints;
				GD.Print($"Enemy announced {extrapoints} extra points!");
				//todo check game end
			}
		}

		_playArea.ReceiveCard(card);
	}

	private async void OnBothCardsPlayed(Card[] cards)
	{

		var winner = Rules.determineWinner(cards[0], cards[1], trumpColor);

		var winnerPile = winner.isPlayerCard ? _playerTrickPile : _enemyTrickPile;



		await ToSignal(
			GetTree().CreateTimer(_playAreaWaitingTimeMillis / 1000f),
			Timer.SignalName.Timeout
		);

		foreach (Card card in cards)
		{
			winnerPile.ReceiveCard(card);
		}



		if (winner.isPlayerCard)
		{
			_playerScore += Rules.Points(cards[0].Value) + Rules.Points(cards[1].Value);
			DealCardsToHand(_playerHand, 1);
			DealCardsToHand(_enemyHand, 1);
		}
		else
		{
			_enemyScore += Rules.Points(cards[0].Value) + Rules.Points(cards[1].Value);
			DealCardsToHand(_enemyHand, 1);
			DealCardsToHand(_playerHand, 1);
		}

		int totalPlayerPoints = _playerScore + _playerExtraPoints;
		int totalEnemyPoints = _enemyScore + _enemyExtraPoints;

		if (_playerScore == 0) totalPlayerPoints = 0;
		if (_enemyScore == 0) totalEnemyPoints = 0;
		GD.Print($"Player score: {totalPlayerPoints}, Enemy score: {totalEnemyPoints}");

		
		if (totalPlayerPoints >= 66)
		{
			GD.Print("Player wins the game!");
		}
		else if (totalEnemyPoints >= 66)
		{
			GD.Print("Enemy wins the game!");
		}
	}
}

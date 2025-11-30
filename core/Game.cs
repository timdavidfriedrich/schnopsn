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
using Schnopsn.Components.bummerl;

public partial class Game : Panel
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
	private TrickPileScore _playerTrickPileScore;
	[Export]
	private TrickPileScore _enemyTrickPileScore;
	[Export]
	private BummerlCounter _playerBummerlCounter;
	[Export]
	private BummerlCounter _enemyBummerlCounter;
	[Export]
	private PlayArea _playArea;
	[Export]
	private DrawPile _drawPile;

	[Export]
	private PackedScene _cardScene;

	private BummerlManager _bummerlManager;

	private Card[] _cards;

	private Card trumpCard;

	private CardColor trumpColor;

	private int _playerScore = 0;
	private int _enemyScore = 0;

	private int _playerExtraPoints = 0;
	private int _enemyExtraPoints = 0;

	private bool _isFirstCardofTrick = true;


	public override async void _Ready()
	{
		// * Allow Game background panel to handle touch input
		MouseFilter = MouseFilterEnum.Stop; 

		InitBummerlFromLastRound();

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

	private void InitBummerlFromLastRound()
    {
		_bummerlManager = BummerlManager.Instance;
		if (_bummerlManager == null)
		{
			GD.PrintErr("BummerlManager instance not found!");
			return;
		}
		_playerBummerlCounter.Value = _bummerlManager.PlayerBummerl;
		_enemyBummerlCounter.Value = _bummerlManager.EnemyBummerl;
    }

	private void SubscribeToSignals()
	{
		_drawPile.DrawPileClicked += OnDrawPileClicked;
		_playerHand.WantsToPlayCard += OnHandWantsToPlayCard;
		_enemyHand.WantsToPlayCard += OnHandWantsToPlayCard;
		_playArea.BothCardsPlayed += OnBothCardsPlayed;
	}

	private void UnsubscribeFromSignals()
	{
		_drawPile.DrawPileClicked -= OnDrawPileClicked;
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
		trumpCard = _drawPile.PeekBottomCard();
		if (trumpCard == null)
		{
			GD.PrintErr("No cards in draw pile to set trump!");
			return;
		}

		trumpColor = trumpCard.Color;

		// Trumpfkarte aufdecken
		trumpCard.FaceUp();

		// Leicht aus dem Stapel rausschieben – hier kannst du
		// mit den Werten spielen, bis es genau so aussieht wie in deinem Wunsch-Screenshot
		var talonPos = _drawPile.GlobalPosition;
		var offset = new Vector2(10, 20); // z.B. 10px rechts, 20px runter
		trumpCard.GlobalPosition = talonPos + offset;

		// Über dem Stapel zeichnen lassen
		trumpCard.ZAsRelative = false;
		trumpCard.ZIndex = _drawPile.ZIndex + 1;

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

	public override void _GuiInput(InputEvent @event)
	{
		bool isTap = @event is InputEventScreenTouch touchEvent && touchEvent.Pressed;
		if (!isTap) return;
		_playerHand.OnTouchOutside();
		AcceptEvent();
	}

	private async void OnHandWantsToPlayCard(Card card, Hand hand)
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
			// 1) alte Trumpf-Daten merken
			var oldTrumpColor = trumpCard.Color;
			var oldTrumpValue = trumpCard.Value;

			// 2) Trumpfkarte im Talon bekommt Daten vom Unter
			trumpCard.WithData(card.Color, card.Value);
			trumpCard.FaceUp();

			// 3) Karte in der Hand bekommt die alten Trumpf-Daten
			card.WithData(oldTrumpColor, oldTrumpValue);
			card.FaceUp();

			// 4) Auswahl zurücksetzen – Karte bleibt in der Hand!
			card.Deselect();          // State -> Idle + Animation
			hand.OnTouchOutside();    // _selectedCard = null

			GD.Print($"{(hand == _playerHand ? "Player" : "Enemy")} performed Unter swap!");

			if (hand == _enemyHand)
			{
				await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);
				_enemyHand.PlayAnyCard();
			}

			// Nur Tausch, KEIN Ausspielen
			return;
		}

		// --- AB HIER: Karte wird wirklich gespielt ---

		// Jetzt erst aus der Hand entfernen
		hand.RemoveCard(card);
		hand.OnTouchOutside(); // Auswahl sicher weg

		if (hand.CheckAnsage(card))
		{
			int extrapoints = 20;
			if (card.Color == trumpColor) extrapoints = 40;
			if (hand == _playerHand)
			{
				_playerExtraPoints += extrapoints;
				GD.Print($"Player announced {extrapoints} extra points!");
			}
			else
			{
				_enemyExtraPoints += extrapoints;
				GD.Print($"Enemy announced {extrapoints} extra points!");
			}
			UpdateScoreUi();
		}

		bool isFirstCardofTrick = _isFirstCardofTrick;

		if (_isFirstCardofTrick)
		{
			_isFirstCardofTrick = false;
		}

		_playArea.ReceiveCard(card);

		if (isFirstCardofTrick && hand == _playerHand)
		{
			_enemyHand.PlayAnyCard();
		}
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

		UpdateScoreUi();

		int totalPlayerPoints = _playerScore + _playerExtraPoints;
		int totalEnemyPoints = _enemyScore + _enemyExtraPoints;

		if (_playerScore == 0) totalPlayerPoints = 0;
		if (_enemyScore == 0) totalEnemyPoints = 0;
		GD.Print($"Player score: {totalPlayerPoints}, Enemy score: {totalEnemyPoints}");

		bool playerWonGame = totalPlayerPoints >= 66;
		bool enemyWonGame = totalEnemyPoints >= 66;

		if (playerWonGame)
		{
			// TODO: Reduce 1, 2 or 3 Bummerl depending on enemy's score
			_bummerlManager.ReducePlayerBummerl(1);
			ResetGame();
			return;
		}
		else if (enemyWonGame)
		{
			// TODO: Reduce 1, 2 or 3 Bummerl depending on player's score
			_bummerlManager.ReduceEnemyBummerl(1);
			ResetGame();
			return;
		}

		// Neuen Stich vorbereiten
		_isFirstCardofTrick = true;

		// Wenn der Gegner den Stich gewonnen hat und das Spiel noch nicht vorbei ist,
		// soll der Gegner den nächsten Stich eröffnen.
		if (!playerWonGame && !enemyWonGame && !winner.isPlayerCard)
		{
			await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);

			if (_enemyHand.HasCards)
			{
				_enemyHand.PlayAnyCard();
			}
		}

	}

	private void OnDrawPileClicked()
    {
        GD.Print("Draw pile clicked.");
    }

	private void ResetGame()
	{
		GD.Print("Resetting game...");
		GetTree().ReloadCurrentScene();
	}


	private void ClearCardsFromReceiver(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Card card)
			{
				card.QueueFree();
			}
		}
	}

	private void UpdateScoreUi()
	{
		int totalPlayerPoints = _playerScore + _playerExtraPoints;
		int totalEnemyPoints = _enemyScore + _enemyExtraPoints;

		// 0-Stich-Regel berücksichtigen, wenn du das auch in der UI willst:
		if (_playerScore == 0) totalPlayerPoints = 0;
		if (_enemyScore == 0) totalEnemyPoints = 0;

		_playerTrickPileScore.SetScore(totalPlayerPoints);
		_enemyTrickPileScore.SetScore(totalEnemyPoints);
	}


}

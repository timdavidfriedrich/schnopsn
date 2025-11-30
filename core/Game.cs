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
	private bool _isTalonClosed = false;
	private bool _talonClosedByPlayer = false;

	private int _playerPointsAtClose = 0;
	private int _enemyPointsAtClose = 0;
	private bool _playerHadTrickAtClose = false;
	private bool _enemyHadTrickAtClose = false;
	private Card _currentLeadCard = null;
	private Hand _currentLeadHand = null;

	private bool IsEndgamePhase => _isTalonClosed || _drawPile.CardCount == 0;

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

		// --- Trumpf-Unter-Tausch (wie bisher) ---
		if (!_isTalonClosed
			&& card.Color == trumpColor
			&& card.Value == CardValue.unter
			&& trumpCard.Value != CardValue.unter
			&& _drawPile.CardCount > 2
			&& _drawPile.ContainsCard(trumpCard))
		{
			// (Untertausch-Logik unverändert)
			var oldTrumpColor = trumpCard.Color;
			var oldTrumpValue = trumpCard.Value;

			trumpCard.WithData(card.Color, card.Value);
			trumpCard.FaceUp();

			card.WithData(oldTrumpColor, oldTrumpValue);
			card.FaceUp();

			card.Deselect();
			hand.OnTouchOutside();

			GD.Print($"{(hand == _playerHand ? "Player" : "Enemy")} performed Unter swap!");

			if (hand == _enemyHand)
			{
				await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);
				// Gegner soll danach eine Karte spielen
				PlayEnemyTurnSecondCardIfNeeded();
			}

			return;
		}

		// --- NEU: Legalitätscheck vor dem Entfernen aus der Hand ---
		if (!IsPlayLegal(hand, card))
		{
			GD.Print("Illegal move prevented (Farb-/Stich-/Trumpfzwang).");
			// Karte bleibt einfach in der Hand; bei Spieler kann man zusätzlich UI machen.
			return;
		}

		// Karte aus der Hand nehmen erst NACH Bestehen des Checks
		hand.RemoveCard(card);
		hand.OnTouchOutside(); // Auswahl sicher weg

		// Ansage-Logik wie bisher
		if (_isFirstCardofTrick && hand.CheckAnsage(card))
		{
			int extrapoints = (card.Color == trumpColor) ? 40 : 20;
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
			_currentLeadCard = card;
			_currentLeadHand = hand;
		}

		_playArea.ReceiveCard(card);

		// Spieler eröffnet Stich -> Gegner spielt eine Karte
		if (isFirstCardofTrick && hand == _playerHand)
		{
			PlayEnemyTurn();
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

			if (!_isTalonClosed && _drawPile.CardCount >= 2)
			{
				DealCardsToHand(_playerHand, 1);
				DealCardsToHand(_enemyHand, 1);
			}
		}
		else
		{
			_enemyScore += Rules.Points(cards[0].Value) + Rules.Points(cards[1].Value);

			if (!_isTalonClosed && _drawPile.CardCount >= 2)
			{
				DealCardsToHand(_enemyHand, 1);
				DealCardsToHand(_playerHand, 1);
			}
		}

		UpdateScoreUi();

		// Gesamtpunkte inkl. Ansagen, mit 0-Stich-Regel
		int totalPlayerPoints = _playerScore + _playerExtraPoints;
		int totalEnemyPoints  = _enemyScore + _enemyExtraPoints;

		if (_playerScore == 0) totalPlayerPoints = 0;
		if (_enemyScore == 0) totalEnemyPoints = 0;

		GD.Print($"Player score: {totalPlayerPoints}, Enemy score: {totalEnemyPoints}");

		// Hat jemand 66 erreicht?
		bool playerReached66 = totalPlayerPoints >= 66;
		bool enemyReached66  = totalEnemyPoints >= 66;

		// Sind alle Karten weg? (keine Handkarten + kein Talon mehr)
		bool allCardsPlayed =
			!_playerHand.HasCards &&
			!_enemyHand.HasCards &&
			(_drawPile.CardCount == 0 || _isTalonClosed);

		if (playerReached66 || enemyReached66 || allCardsPlayed)
		{
			GD.Print("=== ROUND END ===");

			bool playerIsWinner;
			int gamePoints;

			if (_isTalonClosed)
			{
				GD.Print("Talon was CLOSED during the round.");

				bool closerIsPlayer = _talonClosedByPlayer;

				int closerTotalNow     = closerIsPlayer ? totalPlayerPoints : totalEnemyPoints;
				int noncloserTotalNow  = closerIsPlayer ? totalEnemyPoints  : totalPlayerPoints;

				bool closerReached66   = closerIsPlayer ? playerReached66   : enemyReached66;
				bool noncloserReached66= closerIsPlayer ? enemyReached66   : playerReached66;

				int noncloserPointsAtClose =
					closerIsPlayer ? _enemyPointsAtClose : _playerPointsAtClose;

				bool noncloserHadTrickAtClose =
					closerIsPlayer ? _enemyHadTrickAtClose : _playerHadTrickAtClose;

				GD.Print($"Closer = {(closerIsPlayer ? "PLAYER" : "ENEMY")}");
				GD.Print($"Closer reached 66 = {closerReached66}");
				GD.Print($"Non-closer points at close = {noncloserPointsAtClose}");
				GD.Print($"Non-closer had trick at close = {noncloserHadTrickAtClose}");

				bool closerWins;

				// Zudreher hat NICHT 66 → automatische Niederlage
				if (!closerReached66)
				{
					closerWins = false;
					GD.Print("Closer DID NOT reach 66 → automatic loss.");
				}
				else if (noncloserReached66 && noncloserTotalNow > closerTotalNow)
				{
					closerWins = false;
					GD.Print("Both reached 66, but non-closer has MORE points → closer loses.");
				}
				else
				{
					closerWins = true;
					GD.Print("Closer reached 66 and is ahead → closer wins.");
				}

				if (closerWins)
				{
					// Zudreher gewinnt: Punkte des Nicht-Zudrehers beim Zudrehen zählen
					if (noncloserPointsAtClose == 0)
						gamePoints = 3;
					else if (noncloserPointsAtClose < 33)
						gamePoints = 2;
					else
						gamePoints = 1;

					GD.Print($"Closer wins → gamePoints = {gamePoints} (based on non-closer points at close)");

					playerIsWinner = closerIsPlayer;
				}
				else
				{
					// Zudreher verliert: Gegner bekommt 2 oder 3
					if (!noncloserHadTrickAtClose)
					{
						gamePoints = 3;
						GD.Print("Closer loses & non-closer had NO trick at close → gamePoints = 3");
					}
					else
					{
						gamePoints = 2;
						GD.Print("Closer loses & non-closer HAD a trick at close → gamePoints = 2");
					}

					playerIsWinner = !closerIsPlayer;
				}

				GD.Print($"Winner = {(playerIsWinner ? "PLAYER" : "ENEMY")}");
			}
			else
			{
				GD.Print("Talon was OPEN → applying normal rules.");

				// NORMALFALL (nicht zugedreht)
				if (playerReached66 && !enemyReached66)
				{
					playerIsWinner = true;
					GD.Print("Player reached 66 and enemy did not → Player wins.");
				}
				else if (enemyReached66 && !playerReached66)
				{
					playerIsWinner = false;
					GD.Print("Enemy reached 66 and player did not → Enemy wins.");
				}
				else
				{
					// Niemand 66 → letzter Stich
					playerIsWinner = winner.isPlayerCard;
					GD.Print("No one reached 66 → last trick decides winner.");
				}

				int winnerPoints = playerIsWinner ? totalPlayerPoints : totalEnemyPoints;
				int loserPoints  = playerIsWinner ? totalEnemyPoints  : totalPlayerPoints;

				gamePoints = GetGamePointsFromLoser(loserPoints);

				GD.Print($"Based on loser points {loserPoints} → gamePoints = {gamePoints}");
			}

			GD.Print($"Final winner = {(playerIsWinner ? "PLAYER" : "ENEMY")} for {gamePoints} game points.");
			GD.Print("=== END ROUND ===");

			// Spielpunkte abziehen
			if (playerIsWinner)
				_bummerlManager.ReducePlayerBummerl(gamePoints);
			else
				_bummerlManager.ReduceEnemyBummerl(gamePoints);

			ResetGame();
			return;
		}

		// Kein Rundenende -> nächster Stich

		// Neuen Stich vorbereiten
		_isFirstCardofTrick = true;
		_currentLeadCard = null;
		_currentLeadHand = null;

		// Wenn der Gegner den Stich gewonnen hat,
		// soll der Gegner den nächsten Stich eröffnen.
		if (!winner.isPlayerCard)
		{
			await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);

			if (_enemyHand.HasCards)
			{
				PlayEnemyTurn();
			}
		}

	}

	private void OnDrawPileClicked()
	{
		GD.Print("Draw pile clicked.");

		// Talon schon zugedreht? -> nichts tun
		if (_isTalonClosed)
		{
			GD.Print("Talon ist bereits zugedreht.");
			return;
		}

		// Zu wenige Karten im Talon -> laut Regeln meistens nicht mehr zudrehbar
		if (_drawPile.CardCount <= 2)
		{
			GD.Print("Talon kann nicht mehr zugedreht werden (<= 2 Karten).");
			return;
		}

		// Nur am Beginn eines Stiches zudrehen erlauben
		if (!_isFirstCardofTrick)
		{
			GD.Print("Zudrehen ist nur am Beginn eines Stiches erlaubt.");
			return;
		}

		CloseTalon(true);
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
		_playerTrickPileScore.SetScore(GetTotalPoints(true));
		_enemyTrickPileScore.SetScore(GetTotalPoints(false));
	}

	private void CloseTalon(bool closedByPlayer)
	{
		if (_isTalonClosed)
			return;

		_isTalonClosed = true;
		_talonClosedByPlayer = closedByPlayer;

		// *** HIER Punktezustand einfrieren ***
		_playerPointsAtClose = GetTotalPoints(true);
		_enemyPointsAtClose  = GetTotalPoints(false);

		// „hatte Trick“ nur über die Kartenpunkte (_playerScore) checken,
		// Extra-Punkte kommen ja von 20/40 etc.
		_playerHadTrickAtClose = _playerScore > 0;
		_enemyHadTrickAtClose  = _enemyScore > 0;

		_drawPile.CloseTalon(trumpCard);

		GD.Print($"{(closedByPlayer ? "Player" : "Enemy")} hat den Talon zugedreht. " +
				$"(Player@close={_playerPointsAtClose}, Enemy@close={_enemyPointsAtClose})");
	}

	private int GetTotalPoints(bool forPlayer)
	{
		int baseScore = forPlayer ? _playerScore : _enemyScore;
		int extra     = forPlayer ? _playerExtraPoints : _enemyExtraPoints;

		// 0-Stich-Regel: hat jemand keinen Stich, zählen Extra-Punkte nicht
		if (baseScore == 0)
			return 0;

		return baseScore + extra;
	}

	// Wie viele Spielpunkte bekommt der Gewinner, basierend auf den Punkten des Verlierers?
	private int GetGamePointsFromLoser(int loserTotalPoints)
	{
		if (loserTotalPoints == 0)      return 3; // Gegner kein Stich
		if (loserTotalPoints < 33)      return 2; // Gegner < 33 Augen
		return 1;                       // Gegner >= 33 Augen
	}

	private bool IsPlayLegal(Hand hand, Card card)
	{
		string who = (hand == _playerHand) ? "Player" : "Enemy";
		GD.Print($"[LEGALITY] {who} wants to play {card.Color} {card.Value}");

		// Vor Talonende / ohne Zudrehen: alles erlaubt
		if (!IsEndgamePhase)
		{
			GD.Print("[LEGALITY] Talon offen -> freie Wahl erlaubt.");
			return true;
		}

		// Erste Karte des Stiches: immer legal
		if (_isFirstCardofTrick || _currentLeadCard == null)
		{
			GD.Print("[LEGALITY] Erste Karte des Stiches -> freie Wahl erlaubt.");
			return true;
		}

		// Wir sind beim zweiten Spieler des Stiches
		var lead = _currentLeadCard;
		GD.Print($"[LEGALITY] Lead card: {lead.Color} {lead.Value}");

		// Alle Handkarten, die aktuell spielbar sind
		var allCards = hand.CardsInHand
			.Where(c => c.State == CardState.InHand || c.State == CardState.Selected)
			.ToList();

		// 1) Farbzwang
		var sameSuitCards = allCards.Where(c => c.Color == lead.Color).ToList();
		if (sameSuitCards.Any())
		{
			GD.Print("[LEGALITY] Spieler hat Karten in der angespielten Farbe.");

			// Farbe muss bedient werden
			if (card.Color != lead.Color)
			{
				GD.PrintErr($"[LEGALITY] ILLEGAL: Muss Farbe bedienen ({lead.Color}), spielt aber {card.Color}.");
				return false;
			}

			// Stichzwang innerhalb derselben Farbe
			int leadRank = Rules.Rank(lead.Value);
			var higherSameSuit = sameSuitCards
				.Where(c => Rules.Rank(c.Value) > leadRank)
				.ToList();

			if (higherSameSuit.Any())
			{
				GD.Print("[LEGALITY] Spieler hat höhere Karten derselben Farbe -> Stichzwang aktiv.");

				if (Rules.Rank(card.Value) <= leadRank)
				{
					GD.PrintErr($"[LEGALITY] ILLEGAL: Muss stechen (höhere Karte spielen), spielt aber nicht höher.");
					return false;
				}

				GD.Print("[LEGALITY] Legal: Spieler bedient Farbe und sticht höher.");
				return true;
			}

			GD.Print("[LEGALITY] Legal: Spieler bedient Farbe, kein Stichzwang.");
			return true;
		}


		// 2) Trumpfzwang
		var trumps = allCards.Where(c => c.Color == trumpColor).ToList();
		if (trumps.Any())
		{
			GD.Print($"[LEGALITY] Spieler hat keinen {lead.Color}, aber hat Trumpf -> Trumpfzwang aktiv.");

			if (card.Color != trumpColor)
			{
				GD.PrintErr("[LEGALITY] ILLEGAL: Muss einen Trumpf spielen, spielt aber keinen Trumpf.");
				return false;
			}

			GD.Print("[LEGALITY] Legal: Trumpf gespielt.");
			return true;
		}

		// 3) Keine Farbe, kein Trumpf -> freie Wahl
		GD.Print("[LEGALITY] Spieler hat weder Farbe noch Trumpf -> freie Wahl erlaubt.");
		return true;
	}


	private void PlayEnemyTurn()
	{
		if (!_isTalonClosed && _drawPile.CardCount > 2 && _isFirstCardofTrick)
		{
			if (EnemyShouldCloseNow()) CloseTalon(false);
		}

		var card = ChooseCardForEnemy(_enemyHand);
		if (card == null) return;

		OnHandWantsToPlayCard(card, _enemyHand);
	}


	// Falls nach einem Untertausch der Gegner immer noch am Zug ist
	private void PlayEnemyTurnSecondCardIfNeeded()
	{
		if (_isFirstCardofTrick)
			return; // es wurde noch keine Karte gespielt

		var card = ChooseCardForEnemy(_enemyHand);
		if (card == null) return;

		OnHandWantsToPlayCard(card, _enemyHand);
	}

	// EXTREM simple „KI“: nimm die erste legale Karte
	private Card ChooseCardForEnemy(Hand enemyHand)
	{
		// Alle grob spielbaren Karten
		var candidates = enemyHand.CardsInHand
			.Where(c => c.State == CardState.InHand)
			.ToList();

		foreach (var c in candidates)
		{
			if (IsPlayLegal(enemyHand, c))
				return c;
		}

		// Falls keine Karte legal ist (sollte nicht vorkommen),
		// nehmen wir einfach die erste und lassen sie durchgehen.
		return candidates.FirstOrDefault();
	}

	private bool EnemyShouldCloseNow()
	{
		// TODO: Hier später echte KI-Logik einbauen.
		// Aktuell: niemals zudrehen.
		return false;
	}

}

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

public enum Difficulty
{
	Easy,
	Medium,
	Hard
}

public partial class Game : Panel
{
	[Export]
	internal float _playAreaWaitingTimeMillis = 500f;
	[Export]
	internal Hand _playerHand;
	[Export]
	internal Hand _enemyHand;
	[Export]
	internal TrickPile _playerTrickPile;
	[Export]
	internal TrickPile _enemyTrickPile;
	[Export]
	internal TrickPileScore _playerTrickPileScore;
	[Export]
	internal BummerlCounter _playerBummerlCounter;
	[Export]
	internal BummerlCounter _enemyBummerlCounter;
	[Export]
	internal PlayArea _playArea;
	[Export]
	internal DrawPile _drawPile;

	[Export]
	internal PackedScene _cardScene;

	[Export]
	private PackedScene _wonEndDialog;

	[Export]
	private PackedScene _lostEndDialog;

	[Export]
	private TextureButton _closeButton;

	internal BummerlManager _bummerlManager;

	internal DifficultyManager _difficultyManager;

	internal Card[] _cards;

	internal Card trumpCard;

	internal CardColor trumpColor;

	internal int _playerScore = 0;
	internal int _enemyScore = 0;

	internal int _playerExtraPoints = 0;
	internal int _enemyExtraPoints = 0;

	internal bool _isFirstCardofTrick = true;
	internal bool _isTalonClosed = false;
	internal bool _talonClosedByPlayer = false;

	internal int _playerPointsAtClose = 0;
	internal int _enemyPointsAtClose = 0;
	internal bool _playerHadTrickAtClose = false;
	internal bool _enemyHadTrickAtClose = false;
	internal Card _currentLeadCard = null;
	internal Hand _currentLeadHand = null;

	private bool IsEndgamePhase => _isTalonClosed || _drawPile.CardCount == 0;

	public override async void _Ready()
	{
		// * Allow Game background panel to handle touch input
		MouseFilter = MouseFilterEnum.Stop; 

		_difficultyManager = DifficultyManager.Instance;

        AudioManager.Instance?.PlayGameMusic();

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
		_closeButton.Pressed += OnCloseButtonClicked;
	}

	private void UnsubscribeFromSignals()
	{
		_drawPile.DrawPileClicked -= OnDrawPileClicked;
		_playerHand.WantsToPlayCard -= OnHandWantsToPlayCard;
		_enemyHand.WantsToPlayCard -= OnHandWantsToPlayCard;
		_playArea.BothCardsPlayed -= OnBothCardsPlayed;
		_closeButton.Pressed -= OnCloseButtonClicked;
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

		trumpCard.FaceUp();

		var talonPos = _drawPile.GlobalPosition;
		var offset = new Vector2(10, 20);
		trumpCard.GlobalPosition = talonPos + offset;

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

	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest)
		{
			GetViewport()?.SetInputAsHandled();
			GetTree()?.ChangeSceneToFile("res://components/start_menu/StartMenu.tscn");
		}
	}

	private async void OnHandWantsToPlayCard(Card card, Hand hand)
	{
		if (card.State != CardState.InHand && card.State != CardState.Selected)
		{
			GD.PrintErr("Attempted to play a card that is not in hand nor selected!");
			return;
		}

		if (!_isTalonClosed
			&& _isFirstCardofTrick
			&& card.Color == trumpColor
			&& card.Value == CardValue.unter
			&& trumpCard.Value != CardValue.unter
			&& _drawPile.CardCount > 2
			&& _drawPile.ContainsCard(trumpCard))
		{
			var oldTrumpColor = trumpCard.Color;
			var oldTrumpValue = trumpCard.Value;

			trumpCard.WithData(card.Color, card.Value);
			trumpCard.FaceUp();

			card.WithData(oldTrumpColor, oldTrumpValue);
			if (hand == _playerHand)
				card.FaceUp();
			else
				card.FaceDown();
				
			card.Deselect();
			hand.OnTouchOutside();

			GD.Print($"{(hand == _playerHand ? "Player" : "Enemy")} performed Unter swap!");

			if (hand == _enemyHand)
			{
				await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);
				PlayEnemyTurnSecondCardIfNeeded();
			}

			// ! THIS CAUSED A BUG WHERE THE ENEMY WON'T PLAY ANYMORE
			// _isFirstCardofTrick = false;
			// _currentLeadHand = hand;
			// _currentLeadCard = card;

			return;
		}

		if (!IsPlayLegal(hand, card))
		{
			GD.Print("Illegal move prevented (Farb-/Stich-/Trumpfzwang).");
			card.PlayIllegalFeedbackAnimation();
			return;
		}

		hand.RemoveCard(card);
		hand.OnTouchOutside();

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

			if(CheckForImmediateVictoryAfterAnnouncement()) return;
		}

		bool isFirstCardofTrick = _isFirstCardofTrick;
		if (_isFirstCardofTrick)
		{
			_isFirstCardofTrick = false;
			_currentLeadCard = card;
			_currentLeadHand = hand;
		}

		_playArea.ReceiveCard(card);

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

		int totalPlayerPoints = _playerScore + _playerExtraPoints;
		int totalEnemyPoints  = _enemyScore + _enemyExtraPoints;

		if (_playerScore == 0) totalPlayerPoints = 0;
		if (_enemyScore == 0) totalEnemyPoints = 0;

		GD.Print($"Player score: {totalPlayerPoints}, Enemy score: {totalEnemyPoints}");

		// Hat jemand 66 erreicht?
		bool playerReached66 = totalPlayerPoints >= 66;
		bool enemyReached66  = totalEnemyPoints >= 66;

		// Sind alle Karten weg?
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

			EndRoundOrGame(playerIsWinner);
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


	private void OnCloseButtonClicked()
    {
		BummerlManager.Instance.ResetAllBummerl();
		GetTree()?.ChangeSceneToFile("res://components/start_menu/StartMenu.tscn");
    }


	private async void EndRoundOrGame(bool playerIsWinner)
	{
		GD.Print("End game or round...");
		if (playerIsWinner)
        {
			AudioManager.Instance?.PlayWonSound();
        }
		else
        {
			AudioManager.Instance?.PlayLostSound();
        }
		bool hasPlayerWon = _bummerlManager.PlayerBummerl <= 0;
		bool hasEnemyWon  = _bummerlManager.EnemyBummerl <= 0;
		if (hasPlayerWon || hasEnemyWon)
		{
			ShowEndDialog(hasPlayerWon);
		}
		else
		{
			await AudioManager.Instance?.GetSoundFinishedSignal();
			GetTree().ReloadCurrentScene();
		}
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
	}

	private void CloseTalon(bool closedByPlayer)
	{
		if (_isTalonClosed)
			return;

		_isTalonClosed = true;
		_talonClosedByPlayer = closedByPlayer;

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
        // Möglichkeit zum Zudrehen nur am Beginn eines Stiches
        if (!_isTalonClosed && _drawPile.CardCount > 2 && _isFirstCardofTrick)
        {
            if (EnemyShouldCloseNow())
                CloseTalon(false);
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

    // === KI: Karte abhängig von EnemyDifficulty wählen ===
    private Card ChooseCardForEnemy(Hand enemyHand)
    {
        var candidates = enemyHand.CardsInHand
            .Where(c => c.State == CardState.InHand)
            .ToList();

        if (!candidates.Any())
            return null;

        switch (_difficultyManager.EnemyDifficulty)
        {
            case Difficulty.Easy:
                return ChooseCardForEnemyEasy(candidates, enemyHand);
            case Difficulty.Medium:
                return ChooseCardForEnemyMedium(candidates, enemyHand);
            case Difficulty.Hard:
                return ChooseCardForEnemyHard(candidates, enemyHand);
        }

        // Fallback
        return candidates.FirstOrDefault();
    }

    // -------- EASY: zufällig / dumm --------
    private Card ChooseCardForEnemyEasy(List<Card> cards, Hand hand)
    {
        var validCards = cards.Where(c => IsPlayLegal(hand, c)).ToList();
        if (!validCards.Any())
            validCards = cards;

        int idx = (int)(GD.Randi() % (uint)validCards.Count);
        return validCards[idx];
    }

    // -------- MEDIUM: Heuristik + kleiner Minimax im Endspiel --------
    private Card ChooseCardForEnemyMedium(List<Card> cards, Hand hand)
    {
        var validCards = cards.Where(c => IsPlayLegal(hand, c)).ToList();
        if (!validCards.Any())
            validCards = cards;

        // Im Endspiel: kleiner AlphaBeta mit geringer Tiefe (z.B. 2)
        if (IsEndgamePhase)
        {
            Card bestCard = null;
            double bestScore = double.NegativeInfinity;
            int depth = 2;

            foreach (var card in validCards)
            {
                var clonedState = CloneGameStateWithMove(hand, card);
                double score = AlphaBeta(clonedState, depth,
                                         double.NegativeInfinity,
                                         double.PositiveInfinity,
                                         maximizingPlayer: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }

            if (bestCard != null)
                return bestCard;
        }

        // Vor dem Endspiel: einfache Heuristik

        // 1) Trumpf priorisieren
        var trump = validCards.FirstOrDefault(c => c.Color == trumpColor);
        if (trump != null)
            return trump;

        // 2) Falls Nachspieler: Farbe bedienen, wenn möglich
        if (!_isFirstCardofTrick && _currentLeadCard != null)
        {
            var leadColor = _currentLeadCard.Color;
            var follow = validCards.FirstOrDefault(c => c.Color == leadColor);
            if (follow != null)
                return follow;
        }

        // 3) Höchste Punktekarte
        return validCards
            .OrderByDescending(c => Rules.Points(c.Value))
            .First();
    }

    // -------- HARD: Alpha-Beta-Minimax mit dynamischer Tiefe --------
    private Card ChooseCardForEnemyHard(List<Card> cards, Hand hand)
    {
        var validCards = cards.Where(c => IsPlayLegal(hand, c)).ToList();
        if (!validCards.Any())
            validCards = cards;

        // Dynamische Tiefe:
        // - Vor Endspiel: kürzer (3)
        // - Im Endspiel: tiefer (5), weil weniger Karten
        int depth = IsEndgamePhase ? 5 : 3;

        double bestScore = double.NegativeInfinity;
        Card bestCard = null;

        foreach (var card in validCards)
        {
            var clonedState = CloneGameStateWithMove(hand, card);
            double score = AlphaBeta(clonedState, depth,
                                     double.NegativeInfinity,
                                     double.PositiveInfinity,
                                     maximizingPlayer: false);

            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }

        return bestCard ?? validCards.First();
    }

    // Alpha-Beta-Minimax über GameState
    private double AlphaBeta(GameState state, int depth, double alpha, double beta, bool maximizingPlayer)
    {
        if (depth == 0 || state.IsTerminal())
            return EvaluateState(state);

        var currentRole = maximizingPlayer ? PlayerRole.Enemy : PlayerRole.Player;
        var validMoves = state.GetValidMoves(currentRole);

        if (!validMoves.Any())
            return EvaluateState(state);

        if (maximizingPlayer)
        {
            double value = double.NegativeInfinity;
            foreach (var move in validMoves)
            {
                var nextState = state.Clone();
                nextState.ApplyMove(PlayerRole.Enemy, move);
                value = Math.Max(value, AlphaBeta(nextState, depth - 1, alpha, beta, false));
                alpha = Math.Max(alpha, value);
                if (beta <= alpha)
                    break; // Beta-Cutoff
            }
            return value;
        }
        else
        {
            double value = double.PositiveInfinity;
            foreach (var move in validMoves)
            {
                var nextState = state.Clone();
                nextState.ApplyMove(PlayerRole.Player, move);
                value = Math.Min(value, AlphaBeta(nextState, depth - 1, alpha, beta, true));
                beta = Math.Min(beta, value);
                if (beta <= alpha)
                    break; // Alpha-Cutoff
            }
            return value;
        }
    }

    // Bewertung eines Zustands aus Sicht des Gegners (Enemy)
    private double EvaluateState(GameState state)
    {
        int playerPoints = state.GetPlayerPoints();
        int enemyPoints  = state.GetEnemyPoints();

        if (enemyPoints >= 66) return 1000;
        if (playerPoints >= 66) return -1000;

        // Einfacher Punkteabstand
        return enemyPoints - playerPoints;
    }

    // aktuellen Game-Zustand klonen + einen Zug des angegebenen Spielers anwenden
    private GameState CloneGameStateWithMove(Hand hand, Card card)
    {
        var clone = GameState.FromCurrent(this);

        var role = (hand == _playerHand) ? PlayerRole.Player : PlayerRole.Enemy;
        clone.ApplyMove(role, card);

        return clone;
    }

    // -------- Talon-Zudrehen je nach Difficulty --------
    private bool EnemyShouldCloseNow()
    {
        switch (_difficultyManager.EnemyDifficulty)
        {
            case Difficulty.Easy:
                // Einfache KI dreht nie zu
                return false;

            case Difficulty.Medium:
                // Mittlere KI: nur bei klarem Vorsprung & halbwegs starken Karten
                return _enemyScore >= 50
                    && _enemyScore > _playerScore + 20
                    && HasStrongHand(_enemyHand);

            case Difficulty.Hard:
                // Harte KI: aggressiver
                return _enemyScore >= 40
                    && _enemyScore > _playerScore + 10
                    && HasStrongHand(_enemyHand);
        }
        return false;
    }

    private bool HasStrongHand(Hand hand)
    {
        var cards = hand.CardsInHand.ToList();
        int score = cards.Sum(c => Rules.Points(c.Value));
        int trumpCount = cards.Count(c => c.Color == trumpColor);

        // einfache Heuristik: viele Augen + mindestens 2 Trümpfe
        return score >= 25 && trumpCount >= 2;
    }

	private bool CheckForImmediateVictoryAfterAnnouncement()
	{
		// Wenn bereits zugedreht wurde, kannst du entscheiden,
		// ob du trotzdem sofort werten willst. Ich breche sicherheitshalber ab:
		if (_isTalonClosed)
			return false;

		int totalPlayerPoints = GetTotalPoints(true);
		int totalEnemyPoints  = GetTotalPoints(false);

		bool playerReached66 = totalPlayerPoints >= 66;
		bool enemyReached66  = totalEnemyPoints >= 66;

		if (!playerReached66 && !enemyReached66)
			return false; // niemand hat 66, nichts zu tun

		GD.Print("=== ROUND END (immediate after announcement) ===");

		bool playerIsWinner;

		if (playerReached66 && !enemyReached66)
		{
			playerIsWinner = true;
		}
		else if (enemyReached66 && !playerReached66)
		{
			playerIsWinner = false;
		}
		else
		{
			// Beide ≥ 66 → wer mehr hat, gewinnt
			playerIsWinner = totalPlayerPoints >= totalEnemyPoints;
		}

		int winnerPoints = playerIsWinner ? totalPlayerPoints : totalEnemyPoints;
		int loserPoints  = playerIsWinner ? totalEnemyPoints  : totalPlayerPoints;

		int gamePoints = GetGamePointsFromLoser(loserPoints);

		GD.Print($"Immediate winner = {(playerIsWinner ? "PLAYER" : "ENEMY")} for {gamePoints} game points (announcement).");

		if (playerIsWinner)
			_bummerlManager.ReducePlayerBummerl(gamePoints);
		else
			_bummerlManager.ReduceEnemyBummerl(gamePoints);

		EndRoundOrGame(playerIsWinner);
		return true; // WICHTIG: Spiel wurde beendet
	}

	private void ShowEndDialog(bool playerWon)
	{
		Panel dialog = playerWon
			? _wonEndDialog.Instantiate<Panel>()
			: _lostEndDialog.Instantiate<Panel>();
		AddChild(dialog);
		dialog.ZIndex = 4096;
		dialog.SetAnchorsPreset(LayoutPreset.FullRect);
	}

}
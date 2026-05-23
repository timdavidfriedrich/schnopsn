# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Schnopsn** is a single-player Android card game — a digital adaptation of the Austrian trick-taking game "Schnapsen" — built with **Godot 4 (.NET / C#)**. Target platform is Android (mobile-first touch UI).

## Build & Run

This is a Godot 4 project. There are no standalone `dotnet build` or test commands — the game is built via Godot's export pipeline.

**Local dry-run (check what version would be released):**
```bash
bash scripts/bump-version.sh
```

**Validate Fastlane metadata without uploading:**
```bash
bundle install
bundle exec fastlane metadata_only
```

The CI pipeline builds and signs APK/AAB automatically on every push to `main` via `.github/workflows/godot-ci.yml`. It uses `barichello/godot-ci:mono-4.6` (Godot 4.6 + .NET).

## Release Workflow

Releases are driven by commit message prefixes since the last `v*` tag:

| Prefix | Effect |
|---|---|
| `[RELEASE]` | Minor version bump + Play Store internal upload |
| `[HOTFIX]` | Patch version bump + Play Store internal upload |
| anything else | Build artifacts only, no Play Store upload |

`versionCode` = total commit count on `main` (monotonically increasing). After QA, promote to production via the manual `promote-production` GitHub Actions workflow.

## Architecture

### Core (`core/`)

- **`Game.cs`** — The central `Panel` node that orchestrates the entire game loop: card dealing, turn management, legality checking, AI turns, talon closing, score calculation, and round/game end logic. This is the largest and most complex file.
- **`GameState.cs`** — A plain C# class (no Godot node) representing a serializable snapshot of the game for AI search. Used by the Minimax/Alpha-Beta algorithm in `Game.cs`. `GameState.FromCurrent(game)` captures the live state; `Clone()` + `ApplyMove()` simulate moves.
- **`BummerlManager.cs`** — Autoload singleton tracking the Bummerl score (starts at 7, reduced by game points each round). Persists across scene reloads.
- **`DifficultyManager.cs`** — Autoload singleton holding the selected `Difficulty` enum (`Easy`, `Medium`, `Hard`).
- **`AudioManager.cs`** — Autoload singleton for music and sound effects.
- **`Utilities/Rules.cs`** — Pure static helpers: card point values, card rank ordering, and trick winner determination.
- **`Utilities/CardReceiver.cs`** — Interface/base for nodes that accept cards (`ReceiveCard`).
- **`Utilities/ListExtensions.cs`** — Fisher-Yates shuffle extension on `List<T>`.

### Components (`components/`)

Each component has a `.tscn` scene and a `.cs` script:

- **`card/Card.cs`** — Represents a single card. Holds `CardColor`, `CardValue`, `CardState` (InHand, Selected, InPlay, etc.), and handles tween animations for dealing/playing. `WithData()` sets card identity and loads the correct pixel art texture.
- **`hand/Hand.cs`** — Manages a player's hand of cards. Emits `WantsToPlayCard` signal when a card is tapped. `CheckAnsage()` detects 20er/40er announcements (König + Ober of same suit).
- **`play_area/PlayArea.cs`** — The center area where cards are played. Emits `BothCardsPlayed` once both player and enemy cards are present.
- **`draw_pile/DrawPile.cs`** — The face-down talon. Supports `DrawCard()`, `PeekBottomCard()` (trump card), and `CloseTalon()`.
- **`trick_pile/TrickPile.cs`** — Collects won cards per player.
- **`bummerl/BummerlCounter.cs`** — UI widget displaying the Bummerl dots (out of 7).
- **`start_menu/StartMenu.cs`** — Main menu with difficulty selection. Navigates to `Game.tscn`.
- **`end/WonEndDialog.cs`, `end/LostEndDialog.cs`** — Shown when the overall game (Bummerl) ends.

### AI Design

The AI in `Game.cs` has three difficulty tiers:

- **Easy:** Random legal card selection.
- **Medium:** Heuristics (prefer trump, follow suit, high points) + shallow Alpha-Beta (depth 2) in endgame.
- **Hard:** Alpha-Beta Minimax (depth 3 pre-endgame, depth 5 in endgame) + aggressive talon-closing heuristic.

Endgame is defined as: talon closed OR draw pile empty (`IsEndgamePhase`).

The AI uses `GameState` clones for search — `GameState.GetValidMoves()` currently allows all hand cards (legality enforcement is simplified in simulation).

### Signals Flow

1. Player taps a card → `Hand` emits `WantsToPlayCard` → `Game.OnHandWantsToPlayCard`
2. `Game` validates legality, checks for Unter swap / Ansage, moves card to `PlayArea`
3. After player plays → `Game.PlayEnemyTurn()` selects and plays AI card
4. `PlayArea` emits `BothCardsPlayed` → `Game.OnBothCardsPlayed` → scores trick, deals new cards, checks round end

### Scene Graph

Entry point: `StartMenu.tscn` → navigates to `Game.tscn`. On round end (no Bummerl winner), `Game.tscn` reloads itself. On game end, shows `WonEndDialog.tscn` or `LostEndDialog.tscn` as overlay.

## Key Game Rules Implemented

- **Unter swap:** Player holding trump Unter can swap it for the face-up trump card at the start of their lead turn.
- **Ansage (20/40):** Playing König + having the Ober of the same suit scores 20 extra (40 for trump suit). Only valid when leading a trick.
- **Farbzwang / Stichzwang / Trumpfzwang:** Enforced only in endgame phase — must follow suit, must beat if possible, must trump if can't follow.
- **Talon closing (Zudrehen):** Only allowed at the start of a trick when > 2 cards remain. Triggers special scoring rules for the closer.
- **Bummerl scoring:** Each round winner reduces the opponent's Bummerl counter (starting at 7) by 1–3 game points. First to reach 0 loses the game.

## Store Metadata

Play Store listing lives in `fastlane/metadata/android/` (en-US and de-DE). Add store screenshots/images to `fastlane/metadata/android/*/images/` before the first release.

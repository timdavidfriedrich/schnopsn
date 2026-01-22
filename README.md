# Schnopsn

**Schnopsn** is a single-player, digital adaptation of the traditional Austrian card game "Schnapsen" (a highly strategic trick-taking game related to Sixty-Six). Built using the **Godot Generic Engine 4** with **C#**, the project aims to preserve the tactical depth of the original folk game while wrapping it in a charming, low-resolution pixel art aesthetic.

The game is designed as a casual yet mentally stimulating experience where the player competes against an AI opponent to reach 66 points before the other, utilizing German-suited cards (Herz, Schellen, Eichel, Pik).

<p float="left">
  <img width="20%" style="margin:60px;" alt="Main menu" src="https://github.com/user-attachments/assets/d919766a-5a89-4549-9a54-3fbb2b4c2996" />
  <img width="20%" alt="Gameplay screenshot" src="https://github.com/user-attachments/assets/2bce66b2-ee07-4bf5-9414-c409a436e418" />
</p>


### Key Features & Distinctions

**1. Traditional Gameplay meets Retro Digital Esthetics**
The project stands out by fusing a centuries-old analog card game with modern "juicy" game feel and retro visuals.
*   **Authentic Rules:** Implements full Schnapsen rules, including the "Bummerl" scoring system (BummerlCounter.cs), announcements (20er/40er), and the strategic mechanics of closing the Talon ("Zudrehen") implemented in Game.cs.
*   **Visual Style:** The game utilizes low-resolution pixel art assets for cards (`components/card/assets/`) and UI elements, creating a nostalgic atmosphere.
*   **Game Feel:** Despite the pixel art style, the interactions are fluid. Cards utilize tweening for smooth played animations, dealing, and sorting (Card.cs), accompanied by specific audio cues for flipping and flight (AudioManager.cs).

**2. Adaptive Single-Player Difficulty**
Unlike many casual card games that rely purely on RNG, **Schnopsn** features a robust AI opponent with three distinct difficulty levels managed by the `DifficultyManager`:
*   **Easy:** Values random play, suitable for beginners learning the rules.
*   **Medium:** Uses heuristics and a shallow Minimax algorithm during the endgame (Game.cs).
*   **Hard:** Utilizes a deeper **Alpha-Beta Pruning Minimax** algorithm and aggressive strategies for closing the Talon, providing a challenge for veteran players (Game.cs).

**3. Mobile-First Architecture**
The project structure suggests a focus on Android (Schnopsn.csproj references Android targets). The UI components, such as the `StartMenu` and input handling in Game.cs, are designed for touch interaction (taps to select, play, and close the talon).

### Technical Highlights
*   **Engine:** Godot 4 (.NET / C#).
*   **State Management:** A dedicated `GameState` class decouples logic from the UI to facilitate the AI's Minimax simulations (cloning game states to predict future moves).
*   **Component-Based Design:** The game scene is composed of modular components like `Hand`, [`TrickPile`](components/trick_pile/TrickPile.cs), and [`DrawPile`](components/draw_pile/DrawPile.cs), making the code extensible and maintainable.

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

## Release process

Releases are fully automated through GitHub Actions. The pipeline is split into two workflows.

### Build on every push, upload on tag

Every push to `main` runs [.github/workflows/godot-ci.yml](.github/workflows/godot-ci.yml). It **always** builds the debug APK and the signed release AAB and uploads both as GitHub Actions artifacts — useful for sideload testing without touching the Play Store.

Whether the AAB gets shipped to the Play Store depends on the commit messages since the last `v*` tag:

| Commit prefix          | Bump   | Play Store upload |
|------------------------|--------|-------------------|
| `[RELEASE]`            | minor  | yes               |
| `[PATCH]` / `[HOTFIX]` | patch  | yes               |
| anything else          | none   | no — artifacts only |

`[PATCH]` is the routine patch keyword; `[HOTFIX]` is a synonym kept for when you want to flag urgency in the commit history.

When `[RELEASE]`, `[PATCH]`, or `[HOTFIX]` appears in any commit since the last tag, the workflow additionally:

1. Creates and pushes a `vX.Y.Z` tag.
2. Uploads the AAB plus localized metadata and changelog to the **Play Store internal testing** track via Fastlane.
3. Publishes a GitHub Release with the AAB attached.

`versionCode` is the total commit count on `main` — monotonic on every build, so each artifact is uniquely identifiable even when no release was triggered.

### Promotion to production

After QA on the internal track, trigger the [Promote to production](.github/workflows/promote-production.yml) workflow manually from the Actions tab and supply the `versionCode` to promote. Google Play promotes the existing internal artifact — there is no rebuild.

### Fastlane metadata

The Play Store listing lives under [fastlane/metadata/android/](fastlane/metadata/android/) in English (`en-US`) and German (`de-DE`):

- `title.txt`, `short_description.txt`, `full_description.txt` — listing text
- `changelogs/default.txt` — static fallback text shown when a release has no specific notes
- `changelogs/next.txt` — handwritten notes for the **upcoming** release; edit this before a `[RELEASE]`/`[HOTFIX]` push
- `changelogs/<versionCode>.txt` — per-version history; auto-generated on release (see below)
- `images/icon.png`, `images/featureGraphic.png`, `images/phoneScreenshots/*.png` — store assets (**not yet committed** — add when ready)

**Changelog flow per release:**

1. Before pushing your `[RELEASE]`/`[PATCH]`/`[HOTFIX]` commit, write the user-facing notes into `next.txt` for each locale.
2. CI reads `next.txt`. If it's non-empty, that text is uploaded to the Play Store and copied to `<versionCode>.txt` for the in-repo history; `next.txt` is then cleared.
3. If `next.txt` was empty, CI uploads `default.txt` instead and still archives a copy as `<versionCode>.txt`.
4. CI commits the archived `<versionCode>.txt` and cleared `next.txt` back to `main` as `github-actions[bot]`. Run `git pull` before your next push.

The auto-generated `release-notes.md` (a `git log` summary) is used only for the GitHub Release body, never for the Play Store.

To sync metadata without releasing a new binary: `bundle exec fastlane metadata_only`.

### Required GitHub secrets

| Secret                       | Purpose                                                            |
|------------------------------|--------------------------------------------------------------------|
| `ANDROID_KEYSTORE_BASE64`    | Base64-encoded release keystore                                    |
| `ANDROID_KEYSTORE_PASSWORD`  | Keystore password                                                  |
| `ANDROID_KEY_ALIAS`          | Signing key alias                                                  |
| `ANDROID_KEY_PASSWORD`       | Signing key password                                               |
| `GOOGLE_PLAY_JSON_KEY`       | Google Cloud service account JSON with **Release Manager** rights in Google Play Console (Setup → API access). Paste the full JSON. |

### Local dry runs

```bash
# See what the next release would be without pushing anything
bash scripts/bump-version.sh

# Validate Fastlane metadata structure without uploading
bundle install
bundle exec fastlane metadata_only
```

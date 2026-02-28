# Sudoku Web App — Design Concept & Tech Stack

## Tech Stack

### Framework
**Blazor WebAssembly Hosted (.NET 10)**
- A 3-project solution: `Client` (WASM), `Server` (ASP.NET Core), `Shared` (models + interfaces)
- The Client runs as WebAssembly in the browser; the Server hosts it and provides API endpoints
- Tooling: `dotnet new blazorwasm --hosted`
- The Server handles all external API calls (no CORS issues); the Client calls only our own server

### Hosting & Deployment
**A server host is required** (e.g. Railway, Azure App Service, Render, Fly.io — all have free tiers):
- The ASP.NET Core server serves the WASM client and handles API proxying
- On push to `main`, a GitHub Actions workflow builds and deploys the server project
- GitHub Pages is not suitable for hosted Blazor — it only serves static files and cannot run the ASP.NET Core server

### External Sudoku API
**Primary: [API Ninjas Sudoku](https://api-ninjas.com/api/sudoku)** (free, requires API key signup)
- Supports variable grid sizes via `width` (2–4) and `height` (2–4) box parameters
- Supports `difficulty` param: `easy`, `medium`, `hard`
- Returns puzzle grid and solution as 2D arrays (empty cells as `null`)
- Enables grid sizes: 4×4, 6×6, 9×9, 12×12, and 16×16

**Fallback: [sudoku-api](https://sudoku-api.vercel.app/)** (free, no auth required, 9×9 only)
- Endpoint: `GET /api/dosuku` returns board, solution, and difficulty
- Use as a no-auth fallback if API Ninjas is down or rate-limited

**Architecture note:** Abstract the API behind an `IPuzzleProvider` interface so you can swap between API Ninjas, a future local generator, or any other source without touching components. `IPuzzleProvider` lives in the `Shared` project. The implementation (`ApiNinjasPuzzleProvider`) lives on the `Server` — it calls API Ninjas server-to-server with no CORS restrictions. The `Client` calls a `/api/puzzle` endpoint on our own server via `HttpClient`.

**Supported grid configurations (via API Ninjas box params):**

| Box Size | Grid | Symbols | Notes |
|----------|------|---------|-------|
| 2×2 | 4×4 | 1–4 | Beginner / kids |
| 2×3 or 3×2 | 6×6 | 1–6 | Casual |
| 3×3 | 9×9 | 1–9 | Classic (v1 launch) |
| 3×4 or 4×3 | 12×12 | 1–12 | Advanced |
| 4×4 | 16×16 | 1–16 | Expert (uses 1–9 + A–G) |

### CSS / Styling
**Tailwind CSS** with the Tailwind CLI build step:
- **Why Tailwind:** The app is almost entirely custom UI (grid, cells, numpad). Utility classes give precise control without fighting a component framework's defaults. Bootstrap's pre-built components (navbars, cards, forms) would go mostly unused here.
- **Setup:** Install Tailwind via Node (`npm init` + `npm install -D tailwindcss`), configure `tailwind.config.js` to scan your `.razor` and `.html` files for class usage, and run the CLI to compile. Wire this into your build so `dotnet publish` triggers Tailwind's production build (with purging) automatically.
- **Dark mode:** Use Tailwind's `class` strategy (`darkMode: 'class'` in config). Toggle a `.dark` class on `<html>` and use `dark:` prefixed utilities throughout: `bg-amber-50 dark:bg-slate-900`, `text-gray-900 dark:text-gray-100`, etc.
- **CSS custom properties:** Still use them alongside Tailwind for your theme tokens (grid colors, selection highlights, error states). Define them in `app.css` and reference via Tailwind's `theme.extend` or inline `style` attributes where needed.
- **Blazor CSS isolation:** You can still use `.razor.css` scoped files for component-specific styles that don't map well to utilities, but most styling should live in Tailwind classes for consistency.

### State Management
**Built-in Blazor patterns** — no external state library needed at this scale:
- A `GameState` service (scoped/singleton) holding the board, timer, notes, undo history
- Inject it into components via standard DI
- Use `localStorage` interop (via `IJSRuntime`) to persist best times, theme preference, and in-progress games

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│           Client (Blazor WASM)          │
│                                         │
│  ┌──────────┐  ┌──────────┐  ┌───────┐  │
│  │  Pages   │  │Components│  │Services│  │
│  │ Game     │  │ Grid     │  │GameState│ │
│  │ Menu     │  │ Cell     │  │Timer   │  │
│  │ Stats    │  │ NumPad   │  │Hints   │  │
│  │          │  │ Controls │  │Storage │  │
│  └──────────┘  └──────────┘  └───┬───┘  │
│                                  │ HttpClient (/api/puzzle)
└──────────────────────────────────┼──────┘
                                   │
┌──────────────────────────────────┼──────┐
│           Server (ASP.NET Core)  │      │
│                                  ▼      │
│  ┌─────────────────────────────────┐   │
│  │  PuzzleController               │   │
│  │  GET /api/puzzle                │   │
│  │   └─ ApiNinjasPuzzleProvider    │   │
│  └──────────────────┬──────────────┘   │
│                     │ HttpClient        │
└─────────────────────┼───────────────────┘
                      │
┌─────────────────────▼───────────────────┐
│            Shared project               │
│  IPuzzleProvider, GridConfig,           │
│  SudokuPuzzle, Move, Difficulty         │
└─────────────────────────────────────────┘
                      │ server-to-server
                      ▼
        ┌─────────────────────────┐
        │  API Ninjas             │
        └─────────────────────────┘
```

### Key Components

| Component | Responsibility |
|-----------|---------------|
| `SudokuGrid` | N×N grid rendering (driven by `GridConfig`), highlights row/col/box of selected cell. CSS grid uses `repeat(N, 2.5rem)` fixed-size columns; thick box-boundary borders calculated from `BoxWidth`/`BoxHeight` in `SudokuCell.BuildCellClass()` |
| `SudokuCell` | Individual cell — displays value or pencil marks, handles tap/click. Pencil mark layout adapts to grid size (3×3 mini-grid for 9×9, 2×2 for 4×4, 4×4 for 16×16) |
| `NumberPad` | Input panel (1–N + erase), dynamically generates buttons based on grid size. For 16×16, displays 1–9 and A–G. Adapts layout for touch vs. desktop |
| `GameControls` | Undo, hint, new game, pencil mode toggle |
| `TimerDisplay` | Elapsed time, pause/resume |
| `DifficultySelector` | Easy / Medium / Hard selection before starting |
| `GridSizeSelector` | Chooses grid configuration (hidden/disabled in v1, wired up for v2) |

### Key Services

| Service | Lives in | Responsibility |
|---------|----------|---------------|
| `IPuzzleProvider` | Shared | **Interface** abstracting puzzle fetching. Accepts `GridConfig` and `Difficulty`. Implementations can be swapped without touching components |
| `ApiNinjasPuzzleProvider` | Server | Calls API Ninjas server-to-server (no CORS), maps JSON response to `SudokuPuzzle` |
| `PuzzleController` | Server | Exposes `GET /api/puzzle?difficulty=&boxWidth=&boxHeight=`, delegates to `IPuzzleProvider` |
| `GameStateService` | Client | Core game logic — cell selection, value entry, validation, undo stack, win detection. All logic uses `N`, `BoxWidth`, `BoxHeight` — never hardcoded `9`/`3` |
| `HintService` | Client | Analyzes the current board state and suggests the next logical move |
| `TimerService` | Client | Tracks elapsed time, supports pause/resume |
| `LocalStorageService` | Client | JS interop wrapper for persisting settings, stats, theme preference, and in-progress games |

---

## Data Model (Core Types)

All models are generic over grid size. **Never hardcode `9` or `3` anywhere** — always derive from `GridConfig`.

```
GridConfig
├── BoxWidth: int              // e.g. 3 for classic
├── BoxHeight: int             // e.g. 3 for classic
├── Size: int                  // Derived: BoxWidth × BoxHeight (e.g. 9)
├── Symbols: string[]          // ["1".."9"] for 9×9, ["1".."9","A".."G"] for 16×16
└── MaxValue: int              // Same as Size

SudokuPuzzle
├── Config: GridConfig
├── Board: int[N][N]           // Current player state (0 = empty), where N = Config.Size
├── Solution: int[N][N]        // Correct solution
├── GivenCells: bool[N][N]     // true = pre-filled OR correctly solved (locked in)
├── PencilMarks: Set<int>[N][N]  // Notes per cell (Phase 2)
└── Difficulty: enum            // Easy, Medium, Hard
```

**Why `GridConfig` matters:** Every component and service receives the puzzle's `GridConfig` to determine loop bounds, valid values, box boundaries, display symbols, and layout dimensions. This single source of truth prevents scattered magic numbers.

---

## UI/UX Design Direction

### Aesthetic Concept: "Paper & Ink"
A clean, tactile design that evokes the feeling of solving a puzzle on paper — warm backgrounds, subtle textures, confident typography, and satisfying micro-interactions. Not sterile or clinical — it should feel *good* to use.

### Layout Strategy (Mobile-First)

**Mobile / Tablet (portrait):**
```
┌─────────────────────┐
│    Timer   ⚙️ 🌙    │  ← minimal top bar
│                     │
│  ┌───────────────┐  │
│  │               │  │
│  │   9×9 GRID    │  │  ← takes up most of the screen
│  │               │  │
│  └───────────────┘  │
│                     │
│  ┌───────────────┐  │
│  │ 1 2 3 4 5 6   │  │  ← number pad (large touch targets)
│  │ 7 8 9 ✏️ ⌫ 💡  │  │  ← pencil toggle, erase, hint
│  └───────────────┘  │
│  [ Undo ] [New Game]│  ← action buttons
└─────────────────────┘
```

**Desktop (wide):**
```
┌───────────────────────────────────────────┐
│           Timer        ⚙️  🌙             │
│                                           │
│     ┌──────────────┐  ┌──────────────┐    │
│     │              │  │  1  2  3     │    │
│     │              │  │  4  5  6     │    │
│     │   9×9 GRID   │  │  7  8  9     │    │
│     │              │  │              │    │
│     │              │  │  ✏️  ⌫  💡    │    │
│     │              │  │              │    │
│     └──────────────┘  │  Undo        │    │
│                       │  New Game     │    │
│                       └──────────────┘    │
└───────────────────────────────────────────┘
```

### Interaction Design

**Cell selection & input:**
- Tap/click a cell to select it
- The entire row, column, and containing box subtly highlights (box boundaries derived from `GridConfig`)
- All cells with the same number highlight (e.g., select a "5" and every 5 on the board glows)
- Tap a number on the pad to place it; if pencil mode is on, toggle it as a pencil mark
- Keyboard support on desktop: arrow keys to navigate, number keys to enter values, `P` to toggle pencil mode. For 16×16, letter keys A–G also map to values 10–16

**Touch considerations:**
- Minimum 44×44px touch targets on the number pad
- The grid cells themselves should be at least 36px on mobile
- Support long-press on a cell to toggle pencil mode (alternative to the button)
- Haptic feedback via the Vibration API on mobile (subtle pulse on number placement)

**Feedback & animations:**
- Incorrect entries: subtle red flash, optional shake animation
- Correct puzzle completion: satisfying cascade animation across the grid
- Pencil marks: smaller font, lighter color, positioned in a mini-grid within the cell (layout adapts: 2×2 for 4×4 puzzles, 3×3 for 9×9, 4×4 for 16×16)
- Number pad: gentle press animation on tap

### Theming (Dark Mode via Tailwind)

Use Tailwind's `class` dark mode strategy. Toggle `.dark` on the `<html>` element and use `dark:` variants throughout your Razor components. Define your semantic color tokens as CSS custom properties and extend Tailwind's theme to reference them:

**`app.css` custom properties:**

| Token | Light | Dark |
|-------|-------|------|
| `--bg-primary` | warm off-white `#F5F0E8` | deep charcoal `#1A1A2E` |
| `--cell-bg` | white `#FFF` | dark slate `#252540` |
| `--cell-selected` | soft blue `#D4E6F1` | muted indigo `#3D3D6B` |
| `--cell-highlight` | pale yellow `#FFF9E6` | subtle navy `#2A2A4A` |
| `--text-player` | blue `#2E5BBA` | cyan `#64B5F6` |
| `--text-error` | red `#C0392B` | coral `#FF6B6B` |
| `--pencil-mark` | gray `#888` | gray `#999` |

**Usage in Razor components** — mix Tailwind utilities with your tokens:
```html
<div class="bg-[var(--cell-bg)] border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100">
```

Or extend `tailwind.config.js` to map them as named colors (`bg-cell`, `text-player`, etc.) for cleaner markup.

### Hint System Design
A good hint system teaches, not just tells:
1. **Level 1 — Highlight:** "Look at row 3" (highlights the row)
2. **Level 2 — Narrow:** "Cell (3,5) can only be a few values" (highlights the cell)
3. **Level 3 — Reveal:** "Cell (3,5) is 7" (fills it in)

Let the player choose how much help they want. Track hint usage in stats (hints used per game).

---

## Project Structure

```
SudokuApp/                          # Solution root
├── SudokuApp.sln
├── Client/                         # Blazor WASM project
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── app.css             # Tailwind source + CSS custom properties
│   │   │   └── app.min.css         # Compiled Tailwind output (gitignored)
│   │   └── index.html
│   ├── Pages/
│   │   ├── Home.razor              # Main game page
│   │   ├── Menu.razor              # New game / difficulty + grid size select
│   │   └── Stats.razor             # Best times, games played
│   ├── Components/
│   │   ├── SudokuGrid.razor        # Renders N×N grid from GridConfig
│   │   ├── SudokuCell.razor        # Adapts pencil mark layout to grid size
│   │   ├── NumberPad.razor         # Generates 1..N buttons dynamically
│   │   ├── TimerDisplay.razor
│   │   ├── GameControls.razor
│   │   ├── DifficultySelector.razor
│   │   └── GridSizeSelector.razor  # Grid size picker (hidden in v1)
│   ├── Services/
│   │   ├── GameStateService.cs
│   │   ├── HintService.cs
│   │   ├── TimerService.cs
│   │   └── LocalStorageService.cs
│   ├── _Imports.razor
│   ├── App.razor
│   ├── Program.cs                  # DI: HttpClient pointed at Server base address
│   ├── package.json                # Node deps (tailwindcss)
│   └── Client.csproj
│
├── Server/                         # ASP.NET Core project
│   ├── Controllers/
│   │   └── PuzzleController.cs     # GET /api/puzzle → IPuzzleProvider
│   ├── Services/
│   │   └── ApiNinjasPuzzleProvider.cs
│   ├── appsettings.json            # API key placeholder
│   ├── appsettings.Development.json  # Real API key (gitignored)
│   ├── Program.cs                  # Hosts WASM client, registers IPuzzleProvider
│   └── Server.csproj
│
└── Shared/                         # Shared class library
    ├── Models/
    │   ├── GridConfig.cs           # BoxWidth, BoxHeight, Size, Symbols
    │   ├── SudokuPuzzle.cs
    │   ├── Difficulty.cs
    │   └── GameStats.cs
    ├── Services/
    │   └── IPuzzleProvider.cs      # Interface used by both Client and Server
    └── Shared.csproj
```

---

## Development Roadmap

### Phase 1 — Walking Skeleton ✅ Complete
- [x] Define `GridConfig`, `SudokuPuzzle`, `Difficulty` models (`Move` removed — undo not needed with immediate feedback)
- [x] Define `IPuzzleProvider` interface
- [x] Implement `ApiNinjasPuzzleProvider` on the Server
- [x] Restructure into hosted solution (Client / Server / Shared projects)
- [x] Wire up `PuzzleController` on the Server
- [x] Scaffold `SudokuGrid` and `SudokuCell` components
- [x] Render the N×N grid driven by `GridConfig` (9×9 for now), styled with Tailwind
- [x] `GameStateService` (Client singleton — owns all game state, fires `OnChange` for re-render)
- [x] Cell selection (click highlights selected cell; row/column/same-number highlighting)
- [x] Number input (keyboard entry into selected non-given cell)
- [x] Correct/incorrect feedback (correct entries lock in as givens; wrong entries highlight red)
- [x] Win detection (`IsWon` via `GivenCells.All`; "You Win!" card overlay)

### Phase 2 — Core Features
- [ ] Pencil marks (toggle mode + display, layout driven by `GridConfig`)
- [ ] Timer (start on first input, pause, display)
- [ ] Difficulty selection (pass param to `IPuzzleProvider`)
- [ ] NumberPad component generating buttons from `1..N` (still 9 for now, but generic)
- [ ] New game button

### Phase 3 — Polish & UX
- [ ] Dark mode toggle + theme system (Tailwind `dark:` variants)
- [ ] Responsive layout (mobile/tablet/desktop)
- [ ] Keyboard navigation (arrow keys to move between cells)
- [ ] Touch interactions (long-press, haptic feedback)
- [ ] Animations (win celebration cascade)
- [ ] Hint system (3-tier progressive hints, universal techniques)

### Phase 4 — Persistence & Stats
- [ ] Save in-progress game to localStorage
- [ ] Track stats (games completed, best times per difficulty + grid size, hints used)
- [ ] Stats page with personal bests

### Phase 5 — Deployment
- [ ] GitHub Actions workflow for CI/CD (.NET build + Tailwind CSS build)
- [ ] Deploy Server project to a host with free tier (Railway, Render, or Azure App Service)
- [ ] Store API key as a secret in the hosting environment (not in code)
- [ ] README with screenshots and live demo link

### Phase 6 — Variable Grid Sizes (v2)
- [ ] Expose `GridSizeSelector` component in the menu
- [ ] Test and polish 4×4, 6×6, 12×12, 16×16 grid rendering
- [ ] Adapt NumberPad for 16×16 (1–9 + A–G buttons, letter key bindings)
- [ ] Handle 16×16 mobile layout (consider zoom/scroll for small screens)
- [ ] Update stats tracking to segment by grid size
- [ ] Optional: build local puzzle generator as offline/API-free fallback

---

## Risks & Gotchas

**API reliability:** Free Sudoku APIs can go down or rate-limit. Mitigation: the `IPuzzleProvider` abstraction lets you swap to `VercelPuzzleProvider` (9×9 only) automatically on failure. Long-term, build a local puzzle generator for full offline support.

**API key security:** The API Ninjas key now lives on the Server, not in the browser. Store it in `appsettings.Development.json` locally (gitignored) and as an environment variable/secret on the hosting platform. Never commit the real key to the repository.

**Blazor WASM initial load time:** The .NET runtime downloads ~2–5MB on first load. Mitigations: enable Brotli compression, use lazy loading for non-critical assemblies, add a loading screen with a progress indicator. After first load, the runtime is cached.

**Client-side routing on hosted platforms:** Blazor's client-side router needs all paths to serve `index.html`. The ASP.NET Core server handles this automatically via `MapFallbackToFile("index.html")` — no workaround needed as with GitHub Pages.

**Touch vs. mouse input:** Test early and often on real devices. The `@ontouchstart` and `@onclick` events can conflict — you may need to handle both carefully to avoid double-fires.

**16×16 on mobile:** A 16×16 grid with pencil marks is extremely dense on small screens. You'll likely need to allow pinch-to-zoom, horizontal scrolling, or a different interaction model (e.g., a zoom-in view on a selected region). Don't try to solve this until Phase 6 — it's a v2 problem.

**Accessibility:** Don't forget keyboard navigation and ARIA labels on the grid. Screen reader support for Sudoku is tricky but worth considering — at minimum, each cell should announce its position and value. The generic `GridConfig` makes it easy to generate correct ARIA labels like "Row 3, Column 5, Box 2" for any grid size.

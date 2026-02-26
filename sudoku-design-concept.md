# Sudoku Web App — Design Concept & Tech Stack

## Tech Stack

### Frontend Framework
**Blazor WebAssembly (.NET 8 or 9)**
- Runs entirely client-side as WebAssembly — no server needed
- Deploys as static files → perfect for **GitHub Pages** hosting
- Use the `dotnet publish` output (`wwwroot/`) as your GitHub Pages site
- Tooling: `dotnet new blazorwasm` template

### Hosting & Deployment
**GitHub Pages** with a GitHub Actions CI/CD pipeline:
- On push to `main`, build the Blazor WASM project and publish the `wwwroot/` output to the `gh-pages` branch
- Important quirk: GitHub Pages doesn't understand Blazor's client-side routing. You'll need a custom `404.html` that redirects to `index.html` (a well-documented workaround)

### External Sudoku API
**Primary: [API Ninjas Sudoku](https://api-ninjas.com/api/sudoku)** (free, requires API key signup)
- Supports variable grid sizes via `width` (2–4) and `height` (2–4) box parameters
- Supports `difficulty` param: `easy`, `medium`, `hard`
- Returns puzzle grid and solution as 2D arrays (empty cells as `null`)
- Enables grid sizes: 4×4, 6×6, 9×9, 12×12, and 16×16

**Fallback: [sudoku-api](https://sudoku-api.vercel.app/)** (free, no auth required, 9×9 only)
- Endpoint: `GET /api/dosuku` returns board, solution, and difficulty
- Use as a no-auth fallback if API Ninjas is down or rate-limited

**Architecture note:** Abstract the API behind an `IPuzzleProvider` interface so you can swap between API Ninjas, the Vercel fallback, or a future local generator without touching components. Call from Blazor WASM using `HttpClient` injected via DI.

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
┌──────────────────────────────────────────────────────┐
│                    Blazor WASM App                    │
│                                                      │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────┐ │
│  │  Pages   │  │Components│  │     Services        │ │
│  │          │  │          │  │                    │ │
│  │ Game     │  │ Grid     │  │ GameStateService   │ │
│  │ Menu     │  │ Cell     │  │ TimerService       │ │
│  │ Stats    │  │ NumPad   │  │ HintService        │ │
│  │          │  │ Timer    │  │ StorageService     │ │
│  │          │  │ Controls │  │                    │ │
│  └──────────┘  └──────────┘  │ IPuzzleProvider    │ │
│                              │   ├─ ApiNinjasProvider │
│                              │   ├─ VercelFallback   │
│                              │   └─ (LocalGenerator) │
│                              └─────────┬──────────┘ │
│                                        │             │
└────────────────────────────────────────┼─────────────┘
                                         │ HttpClient
                                         ▼
                          ┌──────────────────────────┐
                          │  API Ninjas (primary)     │
                          │  Vercel API (fallback)    │
                          └──────────────────────────┘
```

### Key Components

| Component | Responsibility |
|-----------|---------------|
| `SudokuGrid` | N×N grid rendering (driven by `GridSize`), highlights row/col/box of selected cell. CSS grid uses dynamic `repeat(N, 1fr)` columns; thick borders calculated from `BoxWidth`/`BoxHeight` |
| `SudokuCell` | Individual cell — displays value or pencil marks, handles tap/click. Pencil mark layout adapts to grid size (3×3 mini-grid for 9×9, 2×2 for 4×4, 4×4 for 16×16) |
| `NumberPad` | Input panel (1–N + erase), dynamically generates buttons based on grid size. For 16×16, displays 1–9 and A–G. Adapts layout for touch vs. desktop |
| `GameControls` | Undo, hint, new game, pencil mode toggle |
| `TimerDisplay` | Elapsed time, pause/resume |
| `DifficultySelector` | Easy / Medium / Hard selection before starting |
| `GridSizeSelector` | Chooses grid configuration (hidden/disabled in v1, wired up for v2) |

### Key Services

| Service | Responsibility |
|---------|---------------|
| `IPuzzleProvider` | **Interface** abstracting puzzle fetching. Accepts `GridConfig` (boxWidth, boxHeight, difficulty). Implementations can be swapped without touching components |
| `ApiNinjasPuzzleProvider` | Primary `IPuzzleProvider` — calls API Ninjas with box size + difficulty params, maps JSON response to internal `SudokuPuzzle` model |
| `VercelPuzzleProvider` | Fallback `IPuzzleProvider` — calls the no-auth Vercel API (9×9 only) |
| `GameStateService` | Core game logic — cell selection, value entry, validation, undo stack, win detection. All logic uses `N`, `BoxWidth`, `BoxHeight` — never hardcoded `9`/`3` |
| `HintService` | Analyzes the current board state and suggests the next logical move. Uses universal techniques (naked singles, hidden singles) that work across all grid sizes |
| `TimerService` | Tracks elapsed time, supports pause/resume |
| `LocalStorageService` | JS interop wrapper for persisting settings, stats, theme preference, and in-progress games |

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
├── GivenCells: bool[N][N]     // true = pre-filled, non-editable
├── PencilMarks: Set<int>[N][N]  // Notes per cell
├── Difficulty: enum            // Easy, Medium, Hard
└── MoveHistory: Stack<Move>    // For undo

Move
├── Row, Col: int
├── PreviousValue: int
├── NewValue: int
└── PreviousPencilMarks: Set<int>
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
SudokuApp/
├── wwwroot/
│   ├── css/
│   │   ├── app.css              # Tailwind directives (@tailwind base/components/utilities) + CSS custom properties
│   │   └── app.min.css          # Compiled Tailwind output (generated, gitignored)
│   ├── index.html
│   └── 404.html                 # GitHub Pages SPA redirect hack
├── Pages/
│   ├── Index.razor              # Main game page
│   ├── Menu.razor               # New game / difficulty + grid size select
│   └── Stats.razor              # Best times, games played
├── Components/
│   ├── SudokuGrid.razor         # Renders N×N grid from GridConfig
│   ├── SudokuCell.razor         # Adapts pencil mark layout to grid size
│   ├── NumberPad.razor          # Generates 1..N buttons dynamically
│   ├── TimerDisplay.razor
│   ├── GameControls.razor
│   ├── DifficultySelector.razor
│   └── GridSizeSelector.razor   # Grid size picker (hidden in v1)
├── Services/
│   ├── IPuzzleProvider.cs       # Interface: GetPuzzle(GridConfig, Difficulty)
│   ├── ApiNinjasPuzzleProvider.cs
│   ├── VercelPuzzleProvider.cs  # Fallback, 9×9 only
│   ├── GameStateService.cs
│   ├── HintService.cs
│   ├── TimerService.cs
│   └── LocalStorageService.cs
├── Models/
│   ├── GridConfig.cs            # BoxWidth, BoxHeight, Size, Symbols
│   ├── SudokuPuzzle.cs
│   ├── CellState.cs
│   ├── Move.cs
│   └── GameStats.cs
├── Program.cs                   # Service registration, HttpClient setup, IPuzzleProvider DI
├── SudokuApp.csproj
├── package.json                 # Node deps (tailwindcss)
└── tailwind.config.js           # Content paths: .razor, .html; darkMode: 'class'
```

---

## Development Roadmap

### Phase 1 — Walking Skeleton (9×9 only, variable-size architecture)
Get a puzzle on screen and make it playable, but build on the generic foundation:
- [ ] Scaffold Blazor WASM project + Tailwind CSS setup
- [ ] Define `GridConfig` model and `IPuzzleProvider` interface
- [ ] Implement `ApiNinjasPuzzleProvider` (hardcode `boxWidth=3, boxHeight=3` for now)
- [ ] Implement `VercelPuzzleProvider` as fallback
- [ ] Render the N×N grid driven by `GridConfig` (will be 9×9 at launch)
- [ ] Cell selection + number input (click/tap only)
- [ ] Basic win detection (compare board to solution)

### Phase 2 — Core Features
- [ ] Pencil marks (toggle mode + display, layout driven by `GridConfig`)
- [ ] Undo (move history stack)
- [ ] Timer (start on first input, pause, display)
- [ ] Difficulty selection (pass param to `IPuzzleProvider`)
- [ ] Error highlighting (optional: toggle strict vs. free mode)
- [ ] NumberPad generating buttons from `1..N` (still 9 for now, but generic)

### Phase 3 — Polish & UX
- [ ] Dark mode toggle + theme system (Tailwind `dark:` variants)
- [ ] Responsive layout (mobile/tablet/desktop)
- [ ] Keyboard navigation (arrow keys + number keys)
- [ ] Touch interactions (long-press, haptic feedback)
- [ ] Animations (selection, error shake, win celebration)
- [ ] Hint system (3-tier progressive hints, universal techniques)

### Phase 4 — Persistence & Stats
- [ ] Save in-progress game to localStorage
- [ ] Track stats (games completed, best times per difficulty + grid size, hints used)
- [ ] Stats page with personal bests

### Phase 5 — Deployment
- [ ] GitHub Actions workflow for CI/CD (Node + .NET build steps)
- [ ] Publish to GitHub Pages
- [ ] Add `404.html` redirect for client-side routing
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

**API key exposure:** API Ninjas requires an API key. In a Blazor WASM app, *all code runs in the browser* — the key will be visible in network requests. This is acceptable for a free-tier key with no billing risk, but don't reuse the key for paid services. Document this trade-off in your README.

**Blazor WASM initial load time:** The .NET runtime downloads ~2–5MB on first load. Mitigations: enable Brotli compression, use lazy loading for non-critical assemblies, add a loading screen with a progress indicator. After first load, the runtime is cached.

**GitHub Pages 404 routing:** Blazor's client-side router needs all paths to serve `index.html`. The `404.html` redirect trick works but has a brief flash. Document this in your README.

**Touch vs. mouse input:** Test early and often on real devices. The `@ontouchstart` and `@onclick` events can conflict — you may need to handle both carefully to avoid double-fires.

**16×16 on mobile:** A 16×16 grid with pencil marks is extremely dense on small screens. You'll likely need to allow pinch-to-zoom, horizontal scrolling, or a different interaction model (e.g., a zoom-in view on a selected region). Don't try to solve this until Phase 6 — it's a v2 problem.

**Accessibility:** Don't forget keyboard navigation and ARIA labels on the grid. Screen reader support for Sudoku is tricky but worth considering — at minimum, each cell should announce its position and value. The generic `GridConfig` makes it easy to generate correct ARIA labels like "Row 3, Column 5, Box 2" for any grid size.

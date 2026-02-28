# Sudoku App

A Sudoku web app built with **Blazor WebAssembly** — runs entirely in the browser with no backend required. Designed to support classic 9×9 puzzles now, with a variable-grid architecture ready for 4×4, 6×6, 12×12, and 16×16 in a future release.

**Live demo:** *(coming soon — deploying to GitHub Pages)*

---

## Features

- **Multiple grid sizes** — 9×9 classic at launch; 4×4, 6×6, 12×12, 16×16 planned (v2)
- **Difficulty selection** — Easy, Medium, Hard
- **Pencil marks** — toggle notes mode to track candidate values
- **Undo** — step back through your move history
- **Timer** — tracks elapsed time with pause/resume
- **Progressive hint system** — three levels: highlight a region → narrow to a cell → reveal the value
- **Dark mode** — light/dark theme toggle, persisted across sessions
- **Persistent state** — in-progress games and stats saved to localStorage
- **Stats page** — best times per difficulty, games completed, hints used
- **Fully responsive** — mobile-first layout with touch support

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | Blazor WebAssembly (.NET 10) |
| Styling | Tailwind CSS + CSS custom properties |
| Puzzle API | [API Ninjas Sudoku](https://api-ninjas.com/api/sudoku) (primary) |
| Fallback API | [sudoku-api.vercel.app](https://sudoku-api.vercel.app/) (9×9, no auth) |
| Hosting | GitHub Pages via GitHub Actions |
| Persistence | Browser localStorage (via JS interop) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for Tailwind CSS compilation)

### Run locally

```bash
# Install Node dependencies (Tailwind CSS)
npm install

# Run with hot reload (recommended during development)
dotnet watch
```

The app will open at `https://localhost:5001`.

> **Tailwind:** In a separate terminal, run `npm run watch:css` to recompile styles as you edit `.razor` files.

### Build for production

```bash
npm run build:css       # Compile and purge Tailwind CSS
dotnet publish          # Output static files to wwwroot/
```

---

## Project Structure

```
SudokuApp/
├── Pages/              # Routable pages (Game, Menu, Stats)
├── Components/         # UI components (Grid, Cell, NumberPad, etc.)
├── Services/           # Business logic and DI services
│   ├── IPuzzleProvider.cs          # Interface for puzzle fetching
│   ├── ApiNinjasPuzzleProvider.cs  # Primary API integration
│   ├── VercelPuzzleProvider.cs     # Fallback (9×9 only)
│   ├── GameStateService.cs         # Core game logic
│   ├── HintService.cs
│   ├── TimerService.cs
│   └── LocalStorageService.cs
├── Models/             # GridConfig, SudokuPuzzle, CellState, Move, GameStats
├── wwwroot/            # Static assets (index.html, CSS, 404.html)
├── Program.cs          # App entry point and DI registration
└── sudoku-design-concept.md  # Full design specification
```

---

## Architecture Notes

**`GridConfig` is the single source of truth for grid size.** Every component and service derives loop bounds, box boundaries, valid value ranges, and display symbols from `GridConfig`. Grid dimensions are never hardcoded — this is what makes variable grid sizes possible without rewriting components.

**`IPuzzleProvider` abstracts puzzle fetching.** The primary provider calls API Ninjas (supports all grid sizes); the fallback calls the Vercel API (9×9, no key required). The active implementation is registered in `Program.cs` and can be swapped without touching any component.

**State lives in `GameStateService`.** Components are stateless — they render from the service and dispatch events back to it. LocalStorage persistence is handled through a JS interop wrapper.

---

## Known Trade-offs

**API key visibility:** API Ninjas requires an API key, which will be visible in browser network requests (unavoidable in a client-only WASM app). The free-tier key carries no billing risk, so this is an accepted trade-off. Don't reuse this key for paid services.

**Initial load time:** Blazor WASM downloads the .NET runtime (~2–5 MB) on first visit. Subsequent loads are cached. Brotli compression is enabled to reduce transfer size.

**GitHub Pages routing:** Blazor's client-side router requires all paths to serve `index.html`. A `wwwroot/404.html` redirect is included to handle direct navigation and page refreshes on GitHub Pages.

---

## Roadmap

- [x] Project scaffold + architecture design
- [ ] Phase 1 — Walking skeleton (puzzle on screen, playable 9×9)
- [ ] Phase 2 — Pencil marks, undo, timer, difficulty selection
- [ ] Phase 3 — Dark mode, responsive layout, keyboard/touch support, animations
- [ ] Phase 4 — Persistence and stats
- [ ] Phase 5 — GitHub Pages deployment + CI/CD
- [ ] Phase 6 — Variable grid sizes (4×4, 6×6, 12×12, 16×16)

---

## License

MIT

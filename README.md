# Sudoku Buddy

A Sudoku web app built with **Blazor WebAssembly Hosted** (.NET 10). Puzzles are fetched server-side via API Ninjas, keeping the API key off the client. Designed for classic 9×9 now, with a variable-grid architecture ready for 4×4, 6×6, 12×12, and 16×16 in a future release.

**Live demo:** *(coming soon)*

---

## Current Features (Phase 1)

- **Playable 9×9 grid** — rendered from `GridConfig`, generic enough to support other sizes
- **Cell selection** — click to select; selected row, column, and matching given values highlight
- **Keyboard input** — number keys to enter values, Backspace/Delete to clear
- **Immediate feedback** — correct entries lock in permanently; incorrect entries highlight red
- **Win detection** — "You Win!" overlay when all cells are solved
- **Loading skeleton** — pulsing placeholder grid while the puzzle fetches

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | Blazor WebAssembly Hosted (.NET 10) |
| Styling | Tailwind CSS v4 + CSS custom properties |
| Puzzle source | [API Ninjas Sudoku](https://api-ninjas.com/api/sudoku) (server-side proxy) |
| Hosting | Server host required (Railway, Render, or Azure App Service) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for Tailwind CSS compilation)

### Run locally

```bash
# From Client/ — install Node dependencies and start Tailwind watcher
cd Client
npm install
npm run watch:css

# From Server/ — run with hot reload (in a separate terminal)
cd Server
dotnet watch
```

The app will be available at `http://localhost:5000`.

> The Server project hosts the Client (WASM) and proxies puzzle requests to API Ninjas. An API key is required — add it to `Server/appsettings.Development.json` (gitignored):
> ```json
> { "ApiNinjas": { "ApiKey": "your-key-here" } }
> ```

### Build for production

```bash
# From Client/
npm run build:css

# From Server/
dotnet publish
```

---

## Project Structure

```
SudokuApp/
├── Client/              # Blazor WASM — runs in the browser, calls /api/* on Server
│   ├── Components/      # SudokuGrid, SudokuCell, SudokuGridSkeleton
│   ├── Pages/           # Home.razor (main game page)
│   ├── Services/        # GameStateService
│   └── wwwroot/         # index.html, CSS
├── Server/              # ASP.NET Core — hosts Client, proxies API Ninjas
│   ├── Controllers/     # PuzzleController: GET /api/puzzle
│   └── Services/        # ApiNinjasPuzzleProvider
└── Shared/              # Models and interfaces shared between Client and Server
    ├── Models/          # GridConfig, SudokuPuzzle, Difficulty
    └── Services/        # IPuzzleProvider interface
```

---

## Architecture Notes

**`GridConfig` is the single source of truth for grid size.** Every component and service derives loop bounds, box boundaries, valid value ranges, and display symbols from `GridConfig`. Grid dimensions are never hardcoded.

**`IPuzzleProvider` abstracts puzzle fetching.** The Server calls API Ninjas server-to-server (no CORS issues, API key stays off the client). The Client calls `GET /api/puzzle` on our own server only.

**State lives in `GameStateService`.** A Client-side singleton — components render from it and dispatch events back to it. Components subscribe to `OnChange` for reactive re-renders and implement `IDisposable` to unsubscribe.

**Correct entries are promoted to givens.** When a player enters the right number, it locks in immediately (`GivenCells[row][col] = true`). Win is detected by checking that all `GivenCells` are `true`.

---

## Roadmap

- [x] Phase 1 — Walking skeleton: playable 9×9, cell selection, input, feedback, win detection
- [ ] Phase 2 — Pencil marks, timer, difficulty selection, NumberPad component, new game
- [ ] Phase 3 — Dark mode, responsive layout, keyboard navigation, animations
- [ ] Phase 4 — Persistence and stats (localStorage)
- [ ] Phase 5 — Deployment + CI/CD
- [ ] Phase 6 — Variable grid sizes (4×4, 6×6, 12×12, 16×16)


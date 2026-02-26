# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Mentoring mode

This user is learning. Do not generate code unless explicitly asked. Instead, guide with explanations, ask clarifying questions, point to relevant docs or patterns, and let the user write the code themselves.

## Commands

```bash
dotnet watch          # Run with hot reload (preferred for development)
dotnet run            # Run without hot reload
dotnet build          # Build only
dotnet publish        # Publish for deployment (outputs to wwwroot/)
```

Tailwind CSS requires a separate build step (once Tailwind is configured):
```bash
npm run build:css     # Compile Tailwind → wwwroot/css/app.min.css
npm run watch:css     # Watch mode for Tailwind during development
```

There are no automated tests yet.

## Architecture

This is a **Blazor WebAssembly** app (client-only, no server). All code runs in the browser as WASM. The full design spec lives in [sudoku-design-concept.md](sudoku-design-concept.md) — read it before making architectural decisions.

### Planned structure (not yet fully built)

```
Pages/           # Routable pages: Index (game), Menu, Stats
Components/      # SudokuGrid, SudokuCell, NumberPad, TimerDisplay, GameControls, DifficultySelector
Services/        # IPuzzleProvider interface + implementations, GameStateService, HintService, TimerService, LocalStorageService
Models/          # GridConfig, SudokuPuzzle, CellState, Move, GameStats
```

### Key architectural rules

**GridConfig is the single source of truth for grid size.** Every component and service must derive loop bounds, valid values, box boundaries, display symbols, and layout from `GridConfig`. Never hardcode `9`, `3`, or any grid dimension — always use `Config.Size`, `Config.BoxWidth`, `Config.BoxHeight`.

**`IPuzzleProvider` abstracts puzzle fetching.** Primary: API Ninjas (requires API key). Fallback: Vercel sudoku API (no auth, 9×9 only). Register via DI in `Program.cs` so implementations are swappable without touching components.

**State lives in `GameStateService`** (DI-injected singleton/scoped). Components do not own game state. Use `IJSRuntime` interop via `LocalStorageService` for persistence (in-progress games, stats, theme preference).

### Styling

Tailwind CSS with `darkMode: 'class'` strategy. Toggle `.dark` on `<html>`. CSS custom properties defined in `wwwroot/css/app.css` hold semantic color tokens (`--bg-primary`, `--cell-bg`, `--cell-selected`, etc.). See the design doc for the full token table. Bootstrap (currently bundled) will be replaced by Tailwind.

### Deployment target

GitHub Pages via GitHub Actions. `dotnet publish` output goes to the `gh-pages` branch. A `wwwroot/404.html` redirect is required for Blazor's client-side router to work on GitHub Pages.

### API key exposure

API Ninjas requires a key that will be visible in browser network requests (unavoidable in WASM). This is acceptable for a free-tier key — document the trade-off but don't treat it as a security blocker.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Mentoring mode

This user is learning. Do not generate code unless explicitly asked. Instead, guide with explanations, ask clarifying questions, point to relevant docs or patterns, and let the user write the code themselves.

## Commands

Run from the `Server/` project directory (it hosts the Client):
```bash
dotnet watch          # Run with hot reload (preferred for development)
dotnet run            # Run without hot reload
dotnet build          # Build only
dotnet publish        # Publish for deployment
```

Tailwind CSS lives in the `Client/` project and requires a separate build step (run from `Client/`):
```bash
npm run build:css     # Compile Tailwind → wwwroot/css/app.min.css
npm run watch:css     # Watch mode for Tailwind during development
```

There are no automated tests yet.

## Architecture

This is a **Blazor WebAssembly Hosted** app — a 3-project solution. The full design spec lives in [sudoku-design-concept.md](sudoku-design-concept.md) — read it before making architectural decisions.

```
Client/    # Blazor WASM — runs in the browser, calls /api/* on the Server
Server/    # ASP.NET Core — hosts the Client, proxies API Ninjas, exposes REST endpoints
Shared/    # Class library — models and interfaces shared between Client and Server
```

### Key architectural rules

**GridConfig is the single source of truth for grid size.** Every component and service must derive loop bounds, valid values, box boundaries, display symbols, and layout from `GridConfig`. Never hardcode `9`, `3`, or any grid dimension — always use `Config.Size`, `Config.BoxWidth`, `Config.BoxHeight`.

**Use jagged arrays, never multidimensional.** `SudokuPuzzle` stores `Board`, `Solution`, and `GivenCells` as `int[][]` / `bool[][]`, not `int[,]` / `bool[,]`. `System.Text.Json` cannot serialize multidimensional arrays — using `int[,]` will cause a `NotSupportedException` when the server tries to return a puzzle. Access elements as `board[row][col]`, not `board[row, col]`.

**`SudokuCell` requires position and config.** It takes `Row`, `Col`, and `Config` parameters in addition to `IsGiven` and `Value`. These are used in `BuildCellClass()` to compute thick box-boundary borders via modulo against `Config.BoxWidth` / `Config.BoxHeight`.

**`SudokuGridSkeleton` is the loading placeholder.** Lives in `Client/Components/`. Accepts an optional `GridConfig Config` parameter (defaults to `BoxWidth=3, BoxHeight=3`). Replicates the exact grid structure and box-boundary border logic of `SudokuGrid`/`SudokuCell`, but renders pulsing grey placeholder blocks (`animate-pulse`) instead of values. Used in `Home.razor` while the puzzle fetch is in flight.

**`IPuzzleProvider` abstracts puzzle fetching.** Lives in `Shared`. The implementation (`ApiNinjasPuzzleProvider`) lives on the `Server` and calls API Ninjas server-to-server. The `Client` calls `GET /api/puzzle` on our own server — never directly to external APIs.

**State lives in `GameStateService`** (DI-injected, Client-side, registered as Singleton in `Client/Program.cs`). Components do not own game state. `GameStateService` owns the puzzle, selection, and win state. Key members: `Puzzle`, `SelectedRow`/`SelectedCol` (default `-1`), `IsWon`, `SelectCell()`, `EnterValue()`, `LoadPuzzle()`, `IsCorrect()`, `IsInSelectedRowOrCol()`, `IsSameValueAsSelected()`, and `Action? OnChange`. Components subscribe in `OnInitialized` (`GameState.OnChange += StateHasChanged`) and unsubscribe in `Dispose` — implement `IDisposable` via `@implements IDisposable`. Use `IJSRuntime` interop via `LocalStorageService` for persistence (in-progress games, stats, theme preference).

**Correct entries are promoted to givens.** When `EnterValue` writes a value matching `Solution[row][col]`, it sets `GivenCells[row][col] = true`, locking the cell. `IsWon` is checked after every `EnterValue` via `GivenCells.All(row => row.All(cell => cell))`. There is no undo stack — immediate correct/incorrect feedback makes it unnecessary.

**`SudokuCell.BuildCellClass()` priority order (lowest to highest):** default (given=gray, else white) → row/col highlight (`bg-blue-100`) → selected (`bg-blue-200`) → same given value (`bg-blue-200`) → incorrect (`bg-red-200`, `text-red-700`).

### Styling

Tailwind CSS with `darkMode: 'class'` strategy. Toggle `.dark` on `<html>`. CSS custom properties defined in `Client/wwwroot/css/app.css` hold semantic color tokens (`--bg-primary`, `--cell-bg`, `--cell-selected`, etc.). See the design doc for the full token table.

### Deployment target

A server host with a free tier (Railway, Render, or Azure App Service). The ASP.NET Core Server project is deployed; it serves the compiled WASM Client as static files. GitHub Pages is not suitable — it cannot run the Server.

### API key

The API Ninjas key lives on the Server only — never in the Client. Stored in `Server/appsettings.Development.json` locally (gitignored). In production, injected as an environment variable or host secret.

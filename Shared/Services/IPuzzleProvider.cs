using SudokuApp.Models;

namespace SudokuApp.Services;

public interface IPuzzleProvider
{
    Task<SudokuPuzzle> GetPuzzleAsync(GridConfig config, Difficulty difficulty);
}
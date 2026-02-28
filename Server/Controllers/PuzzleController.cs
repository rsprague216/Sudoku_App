using Microsoft.AspNetCore.Mvc;
using SudokuApp.Models;
using SudokuApp.Services;

namespace SudokuApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PuzzleController : ControllerBase
{
    private readonly IPuzzleProvider _puzzleProvider;

    public PuzzleController(IPuzzleProvider puzzleProvider)
    {
        _puzzleProvider = puzzleProvider;
    }

    [HttpGet]
    public async Task<SudokuPuzzle> Get(
        [FromQuery] int boxWidth = 3,
        [FromQuery] int boxHeight = 3,
        [FromQuery] Difficulty difficulty = Difficulty.Medium)
    {
        var config = new GridConfig { BoxWidth = boxWidth, BoxHeight = boxHeight };
        return await _puzzleProvider.GetPuzzleAsync(config, difficulty);
    }
}

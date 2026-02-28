using System.Net.Http.Json;
using SudokuApp.Models;
using SudokuApp.Services;

namespace SudokuApp.Server.Services;

public class ApiNinjasPuzzleProvider : IPuzzleProvider
{
    private readonly HttpClient _http;

    public ApiNinjasPuzzleProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<SudokuPuzzle> GetPuzzleAsync(GridConfig config, Difficulty difficulty)
    {
        var puzzle = new SudokuPuzzle(config);
        var difficultyStr = difficulty.ToString().ToLower();
        var url = $"https://api.api-ninjas.com/v1/sudokugenerate?difficulty={difficultyStr}&width={config.BoxWidth}&height={config.BoxHeight}";
        

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)        {
            throw new Exception($"API request failed with status code {response.StatusCode}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiNinjasResponse>();
        if (apiResponse == null)
        {
            throw new Exception("API response is null");
        }

        for (int row = 0; row < config.Size; row++)
        {
            for (int col = 0; col < config.Size; col++)
            {
                var value = apiResponse.Puzzle[row][col] ?? 0;
                puzzle.Board[row][col] = value;
                puzzle.Solution[row][col] = apiResponse.Solution[row][col] ?? 0;
                puzzle.GivenCells[row][col] = value != 0;
            }
        }

        return puzzle;
    }

    private class ApiNinjasResponse
    {
        public int?[][] Puzzle { get; set; }
        public int?[][] Solution { get; set; }
        public string Difficulty { get; set; }

        public ApiNinjasResponse()
        {
            Puzzle = Array.Empty<int?[]>();
            Solution = Array.Empty<int?[]>();
            Difficulty = string.Empty;
        }
    }
}
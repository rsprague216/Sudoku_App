namespace SudokuApp.Models;

public class SudokuPuzzle
{
    public GridConfig Config { get; set; }
    public int[][] Solution { get; set; }
    public int[][] Board { get; set; }
    public bool[][] GivenCells { get; set; }

    public SudokuPuzzle(GridConfig config)
    {
        Config = config;
        Solution = new int[Config.Size][];
        Board = new int[Config.Size][];
        GivenCells = new bool[Config.Size][];
        for (int i = 0; i < Config.Size; i++)
        {
            Solution[i] = new int[Config.Size];
            Board[i] = new int[Config.Size];
            GivenCells[i] = new bool[Config.Size];
        }
    }
}

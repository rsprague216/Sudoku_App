using SudokuApp.Models;

namespace SudokuApp.Services;

public class GameStateService
{
    public SudokuPuzzle? Puzzle { get; private set; }

    public int SelectedRow { get; private set; } = -1;
    public int SelectedCol { get; private set; } = -1;

    public bool IsWon { get; private set; } = false;

    public void LoadPuzzle(SudokuPuzzle puzzle)
    {
        Puzzle = puzzle;
        SelectedRow = -1;
        SelectedCol = -1;
        IsWon = false;

        NotifyStateChanged();
    }
    
    public void EnterValue(int value)
    {
        if (Puzzle is null || SelectedRow == -1 || SelectedCol == -1)
            return;
        if (Puzzle.GivenCells[SelectedRow][SelectedCol])
            return;

        Puzzle.Board[SelectedRow][SelectedCol] = value;

        if (IsCorrect(SelectedRow, SelectedCol) && value != 0)
        {
            Puzzle.GivenCells[SelectedRow][SelectedCol] = true; // Mark as given if correct
        }

        IsWon = Puzzle.GivenCells.All(row => row.All(cell => cell));

        NotifyStateChanged();
    }

    public void SelectCell(int row, int col)
    {
        SelectedRow = row;
        SelectedCol = col;

        NotifyStateChanged();
    }

    public bool IsCorrect(int row, int col)
    {
        if (Puzzle is null)
            return true; // No puzzle loaded, so we can't say it's incorrect

        int value = Puzzle.Board[row][col];
        if (value == 0)
            return true; // Empty cells are not incorrect

        int correctValue = Puzzle.Solution[row][col];

        return value == correctValue;
    }

    public bool IsInSelectedRowOrCol(int row, int col)
    {
        return (SelectedRow == row || SelectedCol == col) && (SelectedRow != -1 && SelectedCol != -1);
    }

    public bool IsSameValueAsSelected(int row, int col)
    {
        if (Puzzle is null || SelectedRow == -1 || SelectedCol == -1)
            return false;

        int selectedValue = Puzzle.Board[SelectedRow][SelectedCol];
        int cellValue = Puzzle.Board[row][col];

        bool selectedCellIsGiven = Puzzle.GivenCells[SelectedRow][SelectedCol];
        bool candidateCellIsGiven = Puzzle.GivenCells[row][col];

        return selectedCellIsGiven && candidateCellIsGiven && cellValue == selectedValue;
    }

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
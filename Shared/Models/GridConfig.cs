namespace SudokuApp.Models;

public record GridConfig
{
    public int BoxWidth { get; init; }
    
    public int BoxHeight { get; init; }
    
    public int Size => BoxWidth * BoxHeight;

    public string[] Symbols => Enumerable.Range(1, Size)
        .Select(i => i > 9 ? ((char)('A' + (i - 10))).ToString() : i.ToString())
        .ToArray();
}
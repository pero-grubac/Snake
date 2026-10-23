namespace Snake.Core;

/// <summary>Board dimensions. Every size has its own high score.</summary>
public readonly record struct MapSize
{
    public const int Min = 15;
    public const int Max = 25;

    public static readonly MapSize Default = new(Min, Min);

    public int Rows { get; }
    public int Columns { get; }

    public MapSize(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rows, Max);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, Max);
        Rows = rows;
        Columns = columns;
    }

    public static bool IsValid(int rows, int columns) =>
        rows is >= Min and <= Max && columns is >= Min and <= Max;

    public override string ToString() => $"{Rows}X{Columns}";
}

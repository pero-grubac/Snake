namespace Snake.Core;

public sealed record Direction
{
    public static readonly Direction Left = new(0, -1);
    public static readonly Direction Right = new(0, 1);
    public static readonly Direction Up = new(-1, 0);
    public static readonly Direction Down = new(1, 0);

    public int RowOffset { get; }
    public int ColumnOffset { get; }

    private Direction(int rowOffset, int columnOffset)
    {
        RowOffset = rowOffset;
        ColumnOffset = columnOffset;
    }

    public Direction Opposite() => new(-RowOffset, -ColumnOffset);
}

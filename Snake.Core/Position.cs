namespace Snake.Core;

public sealed record Position(int Row, int Column)
{
    public Position Translate(Direction direction) =>
        new(Row + direction.RowOffset, Column + direction.ColumnOffset);
}

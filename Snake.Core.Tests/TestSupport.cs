namespace Snake.Core.Tests;

/// <summary>
/// Returns scripted values from <see cref="Next(int)"/>, then 0. Food is placed at
/// <c>emptyCells[random.Next(count)]</c> in row-major order, so this decides where it lands.
/// </summary>
internal sealed class ScriptedRandom(params int[] scriptedValues) : Random
{
    private readonly Queue<int> values = new(scriptedValues);

    public override int Next(int maxValue) =>
        values.Count > 0 ? Math.Min(values.Dequeue(), maxValue - 1) : 0;
}

internal static class Boards
{
    /// <summary>A 15x15 board starts with the snake on row 7, columns 1 to 3, heading right.</summary>
    public const int Size = 15;

    public static readonly Position StartHead = new(7, 3);
    public static readonly Position StartTail = new(7, 1);

    /// <summary>The value <see cref="Random.Next(int)"/> must return to put food on <paramref name="target"/>.</summary>
    public static int FoodIndex(int columns, Position target, IEnumerable<Position> occupied)
    {
        int ordinal = target.Row * columns + target.Column;
        return ordinal - occupied.Count(position => position.Row * columns + position.Column < ordinal);
    }

    public static IReadOnlyList<Position> CellsWith(GameState state, GridValue value)
    {
        var cells = new List<Position>();
        for (int row = 0; row < state.Rows; row++)
        {
            for (int column = 0; column < state.Columns; column++)
            {
                if (state[row, column] == value)
                {
                    cells.Add(new Position(row, column));
                }
            }
        }
        return cells;
    }

    public static void Move(GameState state, int times)
    {
        for (int i = 0; i < times; i++)
        {
            state.Move();
        }
    }
}

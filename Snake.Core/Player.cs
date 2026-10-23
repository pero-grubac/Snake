namespace Snake.Core;

/// <summary>One snake on the board, with its own direction, turn buffer and score.</summary>
public sealed class Player
{
    internal Player(int index, Direction direction)
    {
        Index = index;
        Direction = direction;
    }

    internal LinkedList<Position> BodyCells { get; } = new();
    internal LinkedList<Direction> QueuedTurns { get; } = new();

    /// <summary>0 for player one, 1 for player two.</summary>
    public int Index { get; }

    public Direction Direction { get; internal set; }
    public int Score { get; internal set; }
    public bool IsAlive { get; internal set; } = true;

    public Position Head => BodyCells.First!.Value;
    public Position Tail => BodyCells.Last!.Value;
    public int Length => BodyCells.Count;

    /// <summary>Cells from head to tail.</summary>
    public IReadOnlyCollection<Position> Body => BodyCells;
}

namespace Snake.Core;

/// <summary>
/// A simple bot for demo mode: it takes the shortest path to the food unless
/// that would trap it in a pocket smaller than its own body, in which case it
/// moves towards the most open space. In a two-player game it also keeps away
/// from cells the other snake's head could move into.
/// </summary>
public static class AutoPilot
{
    private static readonly Direction[] Directions = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

    public static Direction NextDirection(GameState state, int playerIndex = 0)
    {
        Player me = state.Players[playerIndex];

        List<(Direction Direction, Position Target)> safeMoves = Directions
            .Where(direction => direction != me.Direction.Opposite())
            .Select(direction => (direction, Target: state.NextCell(me.Head, direction)))
            .Where(move => move.Target is not null && IsFree(state, me, move.Target))
            .Select(move => (move.direction, move.Target!))
            .ToList();

        if (safeMoves.Count == 0)
        {
            return me.Direction;
        }

        HashSet<Position> contested = ContestedCells(state, me);
        List<(Direction Direction, Position Target)> calmMoves = safeMoves
            .Where(move => !contested.Contains(move.Target))
            .ToList();
        if (calmMoves.Count > 0)
        {
            safeMoves = calmMoves;
        }

        Direction? towardsFood = FirstStepToFood(state, me);
        if (towardsFood is not null && safeMoves.Any(move => move.Direction == towardsFood))
        {
            Position target = safeMoves.First(move => move.Direction == towardsFood).Target;
            if (ReachableCells(state, me, target) >= me.Length)
            {
                return towardsFood;
            }
        }

        return safeMoves
            .OrderByDescending(move => ReachableCells(state, me, move.Target))
            .First()
            .Direction;
    }

    /// <summary>Its own tail leaves its cell on the next tick, so it counts as free.</summary>
    private static bool IsFree(GameState state, Player me, Position position) =>
        state[position.Row, position.Column] != GridValue.Snake || position == me.Tail;

    private static HashSet<Position> ContestedCells(GameState state, Player me)
    {
        var cells = new HashSet<Position>();
        foreach (Player other in state.Players.Where(player => player != me && player.IsAlive))
        {
            foreach (Direction direction in Directions)
            {
                if (state.NextCell(other.Head, direction) is { } cell)
                {
                    cells.Add(cell);
                }
            }
        }
        return cells;
    }

    private static Direction? FirstStepToFood(GameState state, Player me)
    {
        var firstStep = new Dictionary<Position, Direction>();
        var queue = new Queue<Position>();

        foreach (Direction direction in Directions)
        {
            if (direction != me.Direction.Opposite()
                && state.NextCell(me.Head, direction) is { } next
                && IsFree(state, me, next)
                && firstStep.TryAdd(next, direction))
            {
                queue.Enqueue(next);
            }
        }

        while (queue.Count > 0)
        {
            Position current = queue.Dequeue();
            if (state[current.Row, current.Column] == GridValue.Food)
            {
                return firstStep[current];
            }

            foreach (Direction direction in Directions)
            {
                if (state.NextCell(current, direction) is { } next
                    && IsFree(state, me, next)
                    && next != me.Head
                    && firstStep.TryAdd(next, firstStep[current]))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    private static int ReachableCells(GameState state, Player me, Position start)
    {
        var seen = new HashSet<Position> { start, me.Head };
        var stack = new Stack<Position>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            Position current = stack.Pop();
            foreach (Direction direction in Directions)
            {
                if (state.NextCell(current, direction) is { } next && IsFree(state, me, next) && seen.Add(next))
                {
                    stack.Push(next);
                }
            }
        }

        return seen.Count - 1;
    }
}

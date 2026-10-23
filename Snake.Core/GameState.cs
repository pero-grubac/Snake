namespace Snake.Core;

/// <summary>
/// The whole game: board, snakes, food and score. It knows nothing about WPF;
/// the UI calls <see cref="Move"/> once per tick and draws the result.
/// </summary>
public sealed class GameState
{
    public const int InitialLength = 3;

    /// <summary>How many turns can be queued between two ticks.</summary>
    public const int MaxQueuedTurns = 2;

    private readonly GridValue[,] grid;
    private readonly List<Player> players = new();
    private readonly Random random;

    public GameState(int rows, int columns, Random? random = null)
        : this(rows, columns, GameOptions.Default, random)
    {
    }

    public GameState(MapSize size, Random? random = null)
        : this(size.Rows, size.Columns, GameOptions.Default, random)
    {
    }

    public GameState(MapSize size, GameOptions options, Random? random = null)
        : this(size.Rows, size.Columns, options, random)
    {
    }

    public GameState(int rows, int columns, GameOptions options, Random? random = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.PlayerCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.PlayerCount, 2);
        if (options.PlayerCount == 1)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(columns, InitialLength + 1);
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rows, 4);
            ArgumentOutOfRangeException.ThrowIfLessThan(columns, 2 * InitialLength + 2);
        }

        Rows = rows;
        Columns = columns;
        Options = options;
        grid = new GridValue[rows, columns];
        this.random = random ?? Random.Shared;

        if (options.PlayerCount == 1)
        {
            AddPlayer(Rows / 2, Enumerable.Range(1, InitialLength), Direction.Right);
        }
        else
        {
            AddPlayer(Rows / 3, Enumerable.Range(1, InitialLength), Direction.Right);
            AddPlayer(Rows - 1 - Rows / 3, Enumerable.Range(1, InitialLength).Select(i => Columns - 1 - i), Direction.Left);
        }
        AddFood();
    }

    public int Rows { get; }
    public int Columns { get; }
    public GameOptions Options { get; }
    public IReadOnlyList<Player> Players => players;

    public bool GameOver { get; private set; }

    /// <summary>Single player: the snake filled the whole board.</summary>
    public bool IsWon { get; private set; }

    /// <summary>Two players: index of the player who won, or <c>null</c> for a draw or a game still running.</summary>
    public int? Winner { get; private set; }

    public bool IsPaused { get; private set; }

    public GridValue this[int row, int column] => grid[row, column];

    // Shortcuts for player one, which is all a single-player game needs.
    public Direction Direction => players[0].Direction;
    public int Score => players[0].Score;
    public Position HeadPosition() => players[0].Head;
    public Position TailPosition() => players[0].Tail;
    public IReadOnlyCollection<Position> SnakePositions() => players[0].Body;
    public void ChangeDirection(Direction direction) => ChangeDirection(0, direction);

    public void ChangeDirection(int player, Direction direction)
    {
        Player target = players[player];
        if (CanChangeDirection(target, direction))
        {
            target.QueuedTurns.AddLast(direction);
        }
    }

    public void TogglePause()
    {
        if (!GameOver)
        {
            IsPaused = !IsPaused;
        }
    }

    /// <summary>
    /// The cell one step from <paramref name="from"/>, wrapped around the edges in
    /// <see cref="WallMode.Wrap"/>, or <c>null</c> when it is off a solid board.
    /// </summary>
    public Position? NextCell(Position from, Direction direction)
    {
        Position next = from.Translate(direction);
        if (Options.Walls == WallMode.Wrap)
        {
            return new Position((next.Row + Rows) % Rows, (next.Column + Columns) % Columns);
        }
        return OutsideGrid(next) ? null : next;
    }

    /// <summary>Moves every snake one cell at the same time.</summary>
    public MoveResult Move()
    {
        if (GameOver || IsPaused)
        {
            return MoveResult.None;
        }

        var targets = new Dictionary<Player, Position?>();
        foreach (Player player in players)
        {
            if (player.QueuedTurns.Count > 0)
            {
                player.Direction = player.QueuedTurns.First!.Value;
                player.QueuedTurns.RemoveFirst();
            }
            targets[player] = NextCell(player.Head, player.Direction);
        }

        HashSet<Player> eaters = players
            .Where(player => targets[player] is { } cell && grid[cell.Row, cell.Column] == GridValue.Food)
            .ToHashSet();
        HashSet<Player> dying = FindCollisions(targets, eaters);

        MoveResult result = MoveResult.None;
        List<Player> movers = players.Where(player => !dying.Contains(player)).ToList();
        foreach (Player player in movers.Where(player => !eaters.Contains(player)))
        {
            RemoveTail(player);
        }
        foreach (Player player in movers)
        {
            AddHead(player, targets[player]!);
            if (eaters.Contains(player))
            {
                player.Score++;
                result |= MoveResult.Ate;
            }
        }

        foreach (Player player in dying)
        {
            player.IsAlive = false;
            result |= MoveResult.Died;
        }

        if (dying.Count > 0)
        {
            EndGame(boardFull: false);
        }
        else if (result.HasFlag(MoveResult.Ate) && !AddFood())
        {
            result |= MoveResult.BoardFull;
            EndGame(boardFull: true);
        }

        return result;
    }

    private HashSet<Player> FindCollisions(Dictionary<Player, Position?> targets, HashSet<Player> eaters)
    {
        var dying = new HashSet<Player>();
        bool changed = true;
        while (changed)
        {
            // A tail only leaves its cell when its snake really moves and does not grow.
            HashSet<Position> vacating = players
                .Where(player => !eaters.Contains(player) && !dying.Contains(player))
                .Select(player => player.Tail)
                .ToHashSet();

            changed = false;
            foreach (Player player in players.Where(player => !dying.Contains(player)))
            {
                Position? target = targets[player];
                bool hitsWall = target is null;
                bool hitsBody = target is not null
                    && grid[target.Row, target.Column] == GridValue.Snake
                    && !vacating.Contains(target);
                bool headOn = target is not null
                    && players.Any(other => other != player && targets[other] == target);

                if (hitsWall || hitsBody || headOn)
                {
                    dying.Add(player);
                    changed = true;
                }
            }
        }
        return dying;
    }

    private void EndGame(bool boardFull)
    {
        GameOver = true;
        if (players.Count == 1)
        {
            IsWon = boardFull;
            return;
        }

        List<Player> alive = players.Where(player => player.IsAlive).ToList();
        if (alive.Count == 1)
        {
            Winner = alive[0].Index;
        }
        else if (alive.Count == 2 && players[0].Score != players[1].Score)
        {
            Winner = players[0].Score > players[1].Score ? 0 : 1;
        }
    }

    private void AddPlayer(int row, IEnumerable<int> columnsFromTail, Direction direction)
    {
        var player = new Player(players.Count, direction);
        players.Add(player);
        foreach (int column in columnsFromTail)
        {
            AddHead(player, new Position(row, column));
        }
    }

    private IEnumerable<Position> EmptyPositions()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                if (grid[row, column] == GridValue.Empty)
                {
                    yield return new Position(row, column);
                }
            }
        }
    }

    /// <returns><c>false</c> when there is no empty cell left.</returns>
    private bool AddFood()
    {
        List<Position> empty = EmptyPositions().ToList();
        if (empty.Count == 0)
        {
            return false;
        }

        Position position = empty[random.Next(empty.Count)];
        grid[position.Row, position.Column] = GridValue.Food;
        return true;
    }

    private void AddHead(Player player, Position position)
    {
        player.BodyCells.AddFirst(position);
        grid[position.Row, position.Column] = GridValue.Snake;
    }

    private void RemoveTail(Player player)
    {
        Position tail = player.Tail;
        grid[tail.Row, tail.Column] = GridValue.Empty;
        player.BodyCells.RemoveLast();
    }

    private bool CanChangeDirection(Player player, Direction newDirection)
    {
        if (GameOver || IsPaused || player.QueuedTurns.Count >= MaxQueuedTurns)
        {
            return false;
        }

        Direction lastDirection = player.QueuedTurns.Count == 0 ? player.Direction : player.QueuedTurns.Last!.Value;
        return newDirection != lastDirection && newDirection != lastDirection.Opposite();
    }

    private bool OutsideGrid(Position position) =>
        position.Row < 0 || position.Row >= Rows || position.Column < 0 || position.Column >= Columns;
}

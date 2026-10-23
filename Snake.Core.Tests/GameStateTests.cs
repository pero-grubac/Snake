namespace Snake.Core.Tests;

public class GameStateTests
{
    private static readonly Position[] StartSnake = [new(7, 1), new(7, 2), new(7, 3)];

    /// <summary>A 15x15 game whose first food lands on <paramref name="firstFood"/> (or the far corner).</summary>
    private static GameState NewGame(Position? firstFood = null, params int[] laterFood)
    {
        int first = firstFood is null ? 0 : Boards.FoodIndex(Boards.Size, firstFood, StartSnake);
        return new GameState(Boards.Size, Boards.Size, new ScriptedRandom([first, .. laterFood]));
    }

    [Fact]
    public void New_game_starts_with_a_three_cell_snake_heading_right()
    {
        var state = NewGame();

        Assert.Equal(GameState.InitialLength, state.SnakePositions().Count);
        Assert.Equal(Boards.StartHead, state.HeadPosition());
        Assert.Equal(Boards.StartTail, state.TailPosition());
        Assert.Equal(Direction.Right, state.Direction);
        Assert.Equal(0, state.Score);
        Assert.False(state.GameOver);
        Assert.False(state.IsPaused);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1234)]
    public void New_game_places_exactly_one_food_outside_the_snake(int seed)
    {
        var state = new GameState(Boards.Size, Boards.Size, new Random(seed));

        Position food = Assert.Single(Boards.CellsWith(state, GridValue.Food));
        Assert.DoesNotContain(food, state.SnakePositions());
    }

    [Fact]
    public void Map_size_constructor_uses_its_dimensions()
    {
        var state = new GameState(new MapSize(18, 22));

        Assert.Equal(18, state.Rows);
        Assert.Equal(22, state.Columns);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(5, 3)]
    public void Board_too_small_for_the_snake_is_rejected(int rows, int columns)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(rows, columns));
    }

    [Fact]
    public void Move_shifts_the_snake_one_cell_and_frees_the_old_tail()
    {
        var state = NewGame();

        state.Move();

        Assert.Equal(new Position(7, 4), state.HeadPosition());
        Assert.Equal(new Position(7, 2), state.TailPosition());
        Assert.Equal(GameState.InitialLength, state.SnakePositions().Count);
        Assert.Equal(GridValue.Empty, state[7, 1]);
    }

    [Fact]
    public void Eating_food_grows_the_snake_scores_and_spawns_new_food()
    {
        var state = NewGame(firstFood: new Position(7, 4));

        state.Move();

        Assert.Equal(1, state.Score);
        Assert.Equal(GameState.InitialLength + 1, state.SnakePositions().Count);
        Assert.Equal(Boards.StartTail, state.TailPosition());
        Position food = Assert.Single(Boards.CellsWith(state, GridValue.Food));
        Assert.DoesNotContain(food, state.SnakePositions());
    }

    [Fact]
    public void Snake_cannot_reverse_into_itself()
    {
        var state = NewGame();

        state.ChangeDirection(Direction.Left);
        state.Move();

        Assert.Equal(Direction.Right, state.Direction);
        Assert.Equal(new Position(7, 4), state.HeadPosition());
        Assert.False(state.GameOver);
    }

    [Fact]
    public void Two_quick_turns_are_both_applied_on_the_next_ticks()
    {
        var state = NewGame();

        state.ChangeDirection(Direction.Up);
        state.ChangeDirection(Direction.Left);
        state.Move();
        state.Move();

        Assert.Equal(new Position(6, 2), state.HeadPosition());
        Assert.Equal(Direction.Left, state.Direction);
    }

    [Fact]
    public void Turns_beyond_the_queue_limit_are_dropped()
    {
        var state = NewGame();

        state.ChangeDirection(Direction.Up);
        state.ChangeDirection(Direction.Left);
        state.ChangeDirection(Direction.Down);
        Boards.Move(state, 3);

        Assert.Equal(new Position(6, 1), state.HeadPosition());
        Assert.Equal(Direction.Left, state.Direction);
    }

    [Fact]
    public void Turn_opposite_to_a_queued_turn_is_ignored()
    {
        var state = NewGame();

        state.ChangeDirection(Direction.Up);
        state.ChangeDirection(Direction.Down);
        Boards.Move(state, 2);

        Assert.Equal(new Position(5, 3), state.HeadPosition());
    }

    [Fact]
    public void Hitting_the_wall_ends_the_game()
    {
        var state = NewGame();
        int movesToLastColumn = Boards.Size - 1 - Boards.StartHead.Column;

        Boards.Move(state, movesToLastColumn);
        Assert.False(state.GameOver);

        state.Move();
        Assert.True(state.GameOver);
        Assert.False(state.IsWon);
    }

    [Fact]
    public void Biting_its_own_body_ends_the_game()
    {
        // Food at (7,4) then (7,5): the snake grows to five cells and can reach its body.
        var state = NewGame(firstFood: new Position(7, 4), 106);
        Boards.Move(state, 2);
        Assert.Equal(5, state.SnakePositions().Count);

        state.ChangeDirection(Direction.Down);
        state.Move();
        state.ChangeDirection(Direction.Left);
        state.Move();
        state.ChangeDirection(Direction.Up);
        state.Move();

        Assert.True(state.GameOver);
    }

    [Fact]
    public void Head_may_move_into_the_cell_the_tail_is_leaving()
    {
        var state = NewGame(firstFood: new Position(7, 4));
        state.Move();
        Assert.Equal(4, state.SnakePositions().Count);

        state.ChangeDirection(Direction.Down);
        state.Move();
        state.ChangeDirection(Direction.Left);
        state.Move();
        state.ChangeDirection(Direction.Up);
        state.Move();

        Assert.False(state.GameOver);
        Assert.Equal(new Position(7, 3), state.HeadPosition());
    }

    [Fact]
    public void Paused_game_does_not_move_or_take_turns()
    {
        var state = NewGame();

        state.TogglePause();
        state.ChangeDirection(Direction.Up);
        Boards.Move(state, 5);

        Assert.True(state.IsPaused);
        Assert.Equal(Boards.StartHead, state.HeadPosition());

        state.TogglePause();
        state.Move();

        Assert.False(state.IsPaused);
        Assert.Equal(new Position(7, 4), state.HeadPosition());
    }

    [Fact]
    public void Nothing_changes_after_game_over()
    {
        var state = NewGame();
        Boards.Move(state, Boards.Size);
        Assert.True(state.GameOver);
        Position head = state.HeadPosition();

        state.TogglePause();
        state.ChangeDirection(Direction.Up);
        state.Move();

        Assert.False(state.IsPaused);
        Assert.Equal(head, state.HeadPosition());
    }

    [Fact]
    public void Filling_the_whole_board_wins_the_game()
    {
        // 2x4 board: snake on row 1, columns 1..3. Each food is placed on the next cell
        // of the loop up, left, left, left, down, so five meals fill all eight cells.
        var state = new GameState(2, 4, new ScriptedRandom(3, 2, 1, 0, 0));

        state.ChangeDirection(Direction.Up);
        state.Move();
        state.ChangeDirection(Direction.Left);
        Boards.Move(state, 3);
        Assert.False(state.GameOver);

        state.ChangeDirection(Direction.Down);
        state.Move();

        Assert.True(state.GameOver);
        Assert.True(state.IsWon);
        Assert.Equal(5, state.Score);
        Assert.Equal(8, state.SnakePositions().Count);
        Assert.Empty(Boards.CellsWith(state, GridValue.Food));
    }
}

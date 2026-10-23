namespace Snake.Core.Tests;

public class TwoPlayerTests
{
    private static readonly GameOptions TwoPlayers = new() { PlayerCount = 2 };

    /// <summary>4x8: player one on row 1 (head at column 3, heading right), player two on row 2 (head at column 4, heading left).</summary>
    private static GameState SmallDuel() => new(4, 8, TwoPlayers, new ScriptedRandom(0));

    [Fact]
    public void Snakes_start_on_opposite_sides_facing_each_other()
    {
        var state = new GameState(Boards.Size, Boards.Size, TwoPlayers, new ScriptedRandom(0));

        Assert.Equal(2, state.Players.Count);
        Assert.Equal(new Position(5, 3), state.Players[0].Head);
        Assert.Equal(Direction.Right, state.Players[0].Direction);
        Assert.Equal(new Position(9, 11), state.Players[1].Head);
        Assert.Equal(new Position(9, 13), state.Players[1].Tail);
        Assert.Equal(Direction.Left, state.Players[1].Direction);
        Assert.All(state.Players, player => Assert.True(player.IsAlive));
        Assert.Single(Boards.CellsWith(state, GridValue.Food));
    }

    [Theory]
    [InlineData(3, 10)]
    [InlineData(10, 7)]
    public void Board_too_small_for_two_snakes_is_rejected(int rows, int columns)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(rows, columns, TwoPlayers));
    }

    [Fact]
    public void More_than_two_players_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(15, 15, new GameOptions { PlayerCount = 3 }));
    }

    [Fact]
    public void Each_player_steers_only_their_own_snake()
    {
        var state = new GameState(Boards.Size, Boards.Size, TwoPlayers, new ScriptedRandom(0));

        state.ChangeDirection(1, Direction.Up);
        state.Move();

        Assert.Equal(new Position(5, 4), state.Players[0].Head);
        Assert.Equal(new Position(8, 11), state.Players[1].Head);
    }

    [Fact]
    public void Hitting_the_wall_gives_the_round_to_the_other_player()
    {
        var state = new GameState(Boards.Size, Boards.Size, TwoPlayers, new ScriptedRandom(0));
        state.ChangeDirection(0, Direction.Up);

        Boards.Move(state, 5);
        Assert.False(state.GameOver);
        state.Move();

        Assert.True(state.GameOver);
        Assert.False(state.Players[0].IsAlive);
        Assert.True(state.Players[1].IsAlive);
        Assert.Equal(1, state.Winner);
    }

    [Fact]
    public void Running_into_the_other_snake_loses()
    {
        var state = SmallDuel();

        state.Move();
        state.ChangeDirection(0, Direction.Down);
        state.Move();

        Assert.True(state.GameOver);
        Assert.False(state.Players[0].IsAlive);
        Assert.Equal(1, state.Winner);
    }

    [Fact]
    public void Heads_meeting_on_the_same_cell_is_a_draw()
    {
        var state = SmallDuel();

        state.ChangeDirection(0, Direction.Down);
        state.Move();

        Assert.True(state.GameOver);
        Assert.All(state.Players, player => Assert.False(player.IsAlive));
        Assert.Null(state.Winner);
    }

    [Fact]
    public void Food_goes_to_whoever_reaches_it()
    {
        // Food in front of player two: (9,10) is preceded by 9*15+10 cells minus player one's three.
        var state = new GameState(Boards.Size, Boards.Size, TwoPlayers, new ScriptedRandom(9 * 15 + 10 - 3));

        MoveResult result = state.Move();

        Assert.True(result.HasFlag(MoveResult.Ate));
        Assert.Equal(0, state.Players[0].Score);
        Assert.Equal(1, state.Players[1].Score);
        Assert.Equal(4, state.Players[1].Length);
    }
}

public class WrapTests
{
    private static readonly GameOptions Wrap = new() { Walls = WallMode.Wrap };

    [Fact]
    public void Leaving_the_right_edge_comes_back_on_the_left()
    {
        var state = new GameState(Boards.Size, Boards.Size, Wrap, new ScriptedRandom(0));

        Boards.Move(state, Boards.Size - Boards.StartHead.Column);

        Assert.False(state.GameOver);
        Assert.Equal(new Position(7, 3 + Boards.Size - Boards.StartHead.Column - Boards.Size), state.HeadPosition());
    }

    [Fact]
    public void NextCell_wraps_every_edge()
    {
        var state = new GameState(5, 6, Wrap);

        Assert.Equal(new Position(4, 2), state.NextCell(new Position(0, 2), Direction.Up));
        Assert.Equal(new Position(0, 2), state.NextCell(new Position(4, 2), Direction.Down));
        Assert.Equal(new Position(3, 5), state.NextCell(new Position(3, 0), Direction.Left));
        Assert.Equal(new Position(3, 0), state.NextCell(new Position(3, 5), Direction.Right));
    }

    [Fact]
    public void Solid_walls_have_no_cell_beyond_the_edge()
    {
        var state = new GameState(5, 6);

        Assert.Null(state.NextCell(new Position(0, 2), Direction.Up));
        Assert.Equal(new Position(1, 2), state.NextCell(new Position(0, 2), Direction.Down));
    }

    [Fact]
    public void Biting_itself_still_ends_a_wrapped_game()
    {
        // Food at (7,4) then (7,5), as in the solid-wall test.
        var state = new GameState(Boards.Size, Boards.Size, Wrap, new ScriptedRandom(106, 106));
        Boards.Move(state, 2);

        state.ChangeDirection(Direction.Down);
        state.Move();
        state.ChangeDirection(Direction.Left);
        state.Move();
        state.ChangeDirection(Direction.Up);
        state.Move();

        Assert.True(state.GameOver);
    }
}

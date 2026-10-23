namespace Snake.Core.Tests;

public class AutoPilotTests
{
    private static readonly Position[] StartSnake = [new(7, 1), new(7, 2), new(7, 3)];

    private static GameState GameWithFoodAt(Position food) =>
        new(Boards.Size, Boards.Size, new ScriptedRandom(Boards.FoodIndex(Boards.Size, food, StartSnake)));

    [Fact]
    public void Keeps_going_when_the_food_is_straight_ahead()
    {
        var state = GameWithFoodAt(new Position(7, 10));

        Assert.Equal(Direction.Right, AutoPilot.NextDirection(state));
    }

    [Fact]
    public void Turns_towards_food_above_the_head()
    {
        var state = GameWithFoodAt(new Position(2, 3));

        Assert.Equal(Direction.Up, AutoPilot.NextDirection(state));
    }

    [Fact]
    public void Never_reverses_even_when_the_food_is_behind()
    {
        var state = GameWithFoodAt(new Position(7, 0));

        Direction next = AutoPilot.NextDirection(state);

        Assert.NotEqual(Direction.Left, next);
    }

    [Fact]
    public void Turns_away_from_the_wall()
    {
        var state = GameWithFoodAt(new Position(0, 0));
        Boards.Move(state, Boards.Size - 1 - Boards.StartHead.Column);

        Direction next = AutoPilot.NextDirection(state);
        state.ChangeDirection(next);
        state.Move();

        Assert.Contains(next, new[] { Direction.Up, Direction.Down });
        Assert.False(state.GameOver);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Plays_a_whole_game_and_eats_plenty(int seed)
    {
        var state = new GameState(Boards.Size, Boards.Size, new Random(seed));

        for (int tick = 0; tick < 20_000 && !state.GameOver; tick++)
        {
            state.ChangeDirection(AutoPilot.NextDirection(state));
            state.Move();
        }

        Assert.True(state.Score >= 20, $"Score was only {state.Score}.");
    }
}

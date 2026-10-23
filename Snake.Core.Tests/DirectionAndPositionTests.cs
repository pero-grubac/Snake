namespace Snake.Core.Tests;

public class DirectionAndPositionTests
{
    public static TheoryData<Direction, Direction> Opposites => new()
    {
        { Direction.Up, Direction.Down },
        { Direction.Down, Direction.Up },
        { Direction.Left, Direction.Right },
        { Direction.Right, Direction.Left },
    };

    [Theory]
    [MemberData(nameof(Opposites))]
    public void Opposite_reverses_the_direction(Direction direction, Direction expected)
    {
        Assert.Equal(expected, direction.Opposite());
        Assert.True(expected == direction.Opposite());
    }

    [Fact]
    public void Directions_are_distinct()
    {
        Direction[] all = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

        Assert.Equal(4, all.Distinct().Count());
    }

    [Fact]
    public void Positions_with_the_same_cell_are_equal()
    {
        Assert.Equal(new Position(3, 4), new Position(3, 4));
        Assert.True(new Position(3, 4) == new Position(3, 4));
        Assert.NotEqual(new Position(3, 4), new Position(4, 3));
        Assert.Equal(new Position(3, 4).GetHashCode(), new Position(3, 4).GetHashCode());
    }

    [Theory]
    [InlineData("up", 4, 5)]
    [InlineData("down", 6, 5)]
    [InlineData("left", 5, 4)]
    [InlineData("right", 5, 6)]
    public void Translate_moves_one_cell(string name, int row, int column)
    {
        Direction direction = name switch
        {
            "up" => Direction.Up,
            "down" => Direction.Down,
            "left" => Direction.Left,
            _ => Direction.Right,
        };

        Assert.Equal(new Position(row, column), new Position(5, 5).Translate(direction));
    }
}

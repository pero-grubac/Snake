namespace Snake.Core.Tests;

public class MapSizeTests
{
    [Theory]
    [InlineData(15, 15)]
    [InlineData(25, 25)]
    [InlineData(18, 22)]
    public void Sizes_inside_the_range_are_accepted(int rows, int columns)
    {
        var size = new MapSize(rows, columns);

        Assert.Equal(rows, size.Rows);
        Assert.Equal(columns, size.Columns);
        Assert.True(MapSize.IsValid(rows, columns));
    }

    [Theory]
    [InlineData(14, 15)]
    [InlineData(15, 14)]
    [InlineData(26, 15)]
    [InlineData(15, 26)]
    public void Sizes_outside_the_range_are_rejected(int rows, int columns)
    {
        Assert.False(MapSize.IsValid(rows, columns));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MapSize(rows, columns));
    }

    [Fact]
    public void Default_is_the_smallest_square_map()
    {
        Assert.Equal(new MapSize(15, 15), MapSize.Default);
    }

    [Fact]
    public void ToString_matches_the_on_screen_label()
    {
        Assert.Equal("19X18", new MapSize(19, 18).ToString());
    }
}

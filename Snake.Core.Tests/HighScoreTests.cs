namespace Snake.Core.Tests;

public class HighScoreTableTests
{
    private static readonly MapSize Small = new(15, 15);
    private static readonly MapSize Wide = new(15, 25);

    [Fact]
    public void Unknown_map_has_a_high_score_of_zero()
    {
        Assert.Equal(0, new HighScoreTable().Get(Small));
    }

    [Fact]
    public void Higher_score_becomes_the_new_record()
    {
        var table = new HighScoreTable();

        Assert.True(table.Submit(Small, 7));
        Assert.Equal(7, table.Get(Small));
        Assert.Equal(0, table.Get(Wide));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(5, 3)]
    public void Equal_or_lower_score_is_not_a_record(int best, int score)
    {
        var table = new HighScoreTable();
        table.Submit(Small, best);

        Assert.False(table.Submit(Small, score));
        Assert.Equal(best, table.Get(Small));
    }

    [Fact]
    public void Parse_reads_one_score_per_map()
    {
        var table = HighScoreTable.Parse(["15;15;7", "19;18;2"]);

        Assert.Equal(7, table.Get(Small));
        Assert.Equal(2, table.Get(new MapSize(19, 18)));
    }

    [Fact]
    public void Parse_skips_broken_lines_and_keeps_the_rest()
    {
        var table = HighScoreTable.Parse(["15;15;7", "garbage", "", "15;x;3", "1;2;3;4", "15;25;4"]);

        Assert.Equal(2, table.Scores.Count);
        Assert.Equal(7, table.Get(Small));
        Assert.Equal(4, table.Get(Wide));
    }

    [Theory]
    [InlineData("10;15;5")]
    [InlineData("15;99;5")]
    [InlineData("15;15;-1")]
    public void Parse_ignores_impossible_values(string line)
    {
        Assert.Empty(HighScoreTable.Parse([line]).Scores);
    }

    [Fact]
    public void Parse_keeps_the_best_of_duplicate_lines()
    {
        var table = HighScoreTable.Parse(["15;15;3", "15;15;9", "15;15;4"]);

        Assert.Equal(9, table.Get(Small));
    }

    [Fact]
    public void Serialize_round_trips_in_a_stable_order()
    {
        var table = new HighScoreTable();
        table.Submit(Wide, 4);
        table.Submit(Small, 7);
        table.Submit(new MapSize(20, 16), 1);

        string[] lines = table.Serialize().ToArray();

        Assert.Equal(["15;15;7", "15;25;4", "20;16;1"], lines);
        Assert.Equal(table.Scores, HighScoreTable.Parse(lines).Scores);
    }
}

public sealed class HighScoreStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "blavor-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Missing_file_loads_as_an_empty_table()
    {
        var store = new HighScoreStore(Path.Combine(directory, "score.txt"));

        Assert.Empty(store.Load().Scores);
    }

    [Fact]
    public void Saved_scores_load_back_and_no_temporary_file_is_left()
    {
        var store = new HighScoreStore(Path.Combine(directory, "nested", "score.txt"));
        var table = new HighScoreTable();
        table.Submit(new MapSize(15, 15), 12);

        store.Save(table);

        Assert.Equal(12, store.Load().Get(new MapSize(15, 15)));
        Assert.Equal(["score.txt"], Directory.GetFiles(Path.GetDirectoryName(store.Path)!).Select(Path.GetFileName));
    }

    [Fact]
    public void Saving_replaces_the_previous_file()
    {
        var store = new HighScoreStore(Path.Combine(directory, "score.txt"));
        var table = new HighScoreTable();
        table.Submit(new MapSize(15, 15), 3);
        store.Save(table);

        table.Submit(new MapSize(15, 15), 8);
        store.Save(table);

        Assert.Equal(["15;15;8"], File.ReadAllLines(store.Path));
    }

    [Fact]
    public void Default_path_is_in_the_user_profile()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(localAppData, HighScoreStore.DefaultPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("Blavor", "score.txt"), HighScoreStore.DefaultPath, StringComparison.Ordinal);
    }
}

namespace Snake.Core.Tests;

public class GameSpeedTests
{
    [Fact]
    public void Fixed_levels_get_faster_from_slow_to_fast()
    {
        TimeSpan slow = GameSpeed.TickInterval(SpeedLevel.Slow, 0);
        TimeSpan normal = GameSpeed.TickInterval(SpeedLevel.Normal, 0);
        TimeSpan fast = GameSpeed.TickInterval(SpeedLevel.Fast, 0);

        Assert.True(slow > normal && normal > fast);
        Assert.Equal(TimeSpan.FromMilliseconds(100), normal);
    }

    [Theory]
    [InlineData(SpeedLevel.Slow)]
    [InlineData(SpeedLevel.Normal)]
    [InlineData(SpeedLevel.Fast)]
    public void Fixed_levels_ignore_the_score(SpeedLevel level)
    {
        Assert.Equal(GameSpeed.TickInterval(level, 0), GameSpeed.TickInterval(level, 40));
    }

    [Fact]
    public void Progressive_speeds_up_with_every_point_until_the_limit()
    {
        Assert.Equal(GameSpeed.ProgressiveStart, GameSpeed.TickInterval(SpeedLevel.Progressive, 0));
        Assert.Equal(GameSpeed.ProgressiveStart - GameSpeed.ProgressiveStep * 5, GameSpeed.TickInterval(SpeedLevel.Progressive, 5));
        Assert.Equal(GameSpeed.Fastest, GameSpeed.TickInterval(SpeedLevel.Progressive, 500));
    }
}

public class KeyBindingsTests
{
    [Fact]
    public void Defaults_have_no_conflicts()
    {
        Assert.Empty(KeyBindings.Default.Conflicts());
        Assert.Equal(Enum.GetValues<GameAction>().Length, KeyBindings.Default.All.Count);
    }

    [Fact]
    public void Screenshot_shares_its_key_with_down_by_default()
    {
        Assert.Equal(
            [GameAction.P1Down, GameAction.Screenshot],
            KeyBindings.Default.ActionsFor("s").Order());
    }

    [Fact]
    public void Rebinding_to_a_free_key_changes_only_that_action()
    {
        KeyBindings bindings = KeyBindings.Default.Rebind(GameAction.Pause, "Space");

        Assert.Equal("Space", bindings[GameAction.Pause]);
        Assert.Equal("P", KeyBindings.Default[GameAction.Pause]);
        Assert.Equal("F", bindings[GameAction.Fullscreen]);
    }

    [Fact]
    public void Rebinding_to_a_taken_key_swaps_the_two()
    {
        KeyBindings bindings = KeyBindings.Default.Rebind(GameAction.P1Up, "Up");

        Assert.Equal("Up", bindings[GameAction.P1Up]);
        Assert.Equal("W", bindings[GameAction.P2Up]);
        Assert.Empty(bindings.Conflicts());
    }

    [Fact]
    public void Escape_cannot_be_bound()
    {
        Assert.Same(KeyBindings.Default, KeyBindings.Default.Rebind(GameAction.Pause, "Escape"));
        Assert.Same(KeyBindings.Default, KeyBindings.Default.Rebind(GameAction.Pause, " "));
    }

    [Fact]
    public void A_rebind_that_would_still_clash_is_refused()
    {
        // Pause would get S, which player one's Down also uses.
        Assert.Same(KeyBindings.Default, KeyBindings.Default.Rebind(GameAction.Screenshot, "P"));
    }

    [Fact]
    public void Saved_bindings_are_merged_with_the_defaults()
    {
        KeyBindings bindings = KeyBindings.From(new Dictionary<GameAction, string> { [GameAction.Pause] = "Space" });

        Assert.Equal("Space", bindings[GameAction.Pause]);
        Assert.Equal("W", bindings[GameAction.P1Up]);
    }

    [Fact]
    public void Clashing_saved_bindings_fall_back_to_the_defaults()
    {
        KeyBindings bindings = KeyBindings.From(new Dictionary<GameAction, string> { [GameAction.Pause] = "W" });

        Assert.Same(KeyBindings.Default, bindings);
        Assert.Same(KeyBindings.Default, KeyBindings.From(null));
    }
}

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "blavor-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Missing_file_gives_the_defaults()
    {
        Assert.Same(UserSettings.Default, new SettingsStore(FilePath).Load());
    }

    [Fact]
    public void Settings_round_trip()
    {
        var store = new SettingsStore(FilePath);
        var settings = new UserSettings
        {
            Map = new MapSize(20, 24),
            Speed = SpeedLevel.Progressive,
            Walls = WallMode.Wrap,
            Sound = false,
            Keys = KeyBindings.Default.Rebind(GameAction.Pause, "Space"),
        };

        store.Save(settings);
        UserSettings loaded = store.Load();

        Assert.Equal(settings.Map, loaded.Map);
        Assert.Equal(SpeedLevel.Progressive, loaded.Speed);
        Assert.Equal(WallMode.Wrap, loaded.Walls);
        Assert.False(loaded.Sound);
        Assert.Equal("Space", loaded.Keys[GameAction.Pause]);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "Speed": "Warp" }""")]
    public void Broken_file_gives_the_defaults(string json)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, json);

        Assert.Same(UserSettings.Default, new SettingsStore(FilePath).Load());
    }

    [Fact]
    public void Impossible_map_size_falls_back_to_the_default_map_only()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, """{ "Rows": 99, "Columns": 15, "Speed": "Fast" }""");

        UserSettings loaded = new SettingsStore(FilePath).Load();

        Assert.Equal(MapSize.Default, loaded.Map);
        Assert.Equal(SpeedLevel.Fast, loaded.Speed);
    }
}

public class SoundTests
{
    [Fact]
    public void Wav_has_a_valid_pcm_header_and_the_right_length()
    {
        byte[] wav = ToneSynth.Wav([new Note(440, 100), new Note(0, 50)]);
        int samples = ToneSynth.SampleRate * 100 / 1000 + ToneSynth.SampleRate * 50 / 1000;

        Assert.Equal("RIFF"u8.ToArray(), wav[..4]);
        Assert.Equal("WAVE"u8.ToArray(), wav[8..12]);
        Assert.Equal(1, BitConverter.ToInt16(wav, 20));
        Assert.Equal(ToneSynth.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal(samples * 2, BitConverter.ToInt32(wav, 40));
        Assert.Equal(44 + samples * 2, wav.Length);
    }

    [Fact]
    public void A_rest_is_silent()
    {
        byte[] wav = ToneSynth.Wav([new Note(0, 20)]);

        Assert.All(wav[44..], value => Assert.Equal(0, value));
    }

    [Theory]
    [MemberData(nameof(AllCues))]
    public void Every_cue_has_a_sound(SoundCue cue)
    {
        byte[] wav = SoundBank.WavFor(cue);

        Assert.True(wav.Length > 44);
        Assert.Same(wav, SoundBank.WavFor(cue));
        Assert.NotEmpty(SoundBank.NotesFor(cue));
    }

    public static TheoryData<SoundCue> AllCues => new(Enum.GetValues<SoundCue>());
}

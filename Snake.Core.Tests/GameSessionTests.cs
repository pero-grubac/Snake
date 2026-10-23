namespace Snake.Core.Tests;

public class GameSessionTests
{
    /// <summary>A session whose every board puts the first food at <paramref name="foodIndex"/>.</summary>
    private static GameSession NewSession(UserSettings? settings = null, bool demo = false, int foodIndex = 0) =>
        new(settings ?? UserSettings.Default, new HighScoreTable(), demo, () => new ScriptedRandom(foodIndex));

    private static GameSession Playing(int players = 1, UserSettings? settings = null, bool demo = false, int foodIndex = 0)
    {
        GameSession session = NewSession(settings, demo, foodIndex);
        session.StartRound(players);
        session.BeginPlaying();
        return session;
    }

    private static void TickUntilOver(GameSession session)
    {
        for (int i = 0; i < 10_000 && session.Phase == SessionPhase.Playing; i++)
        {
            session.Tick();
        }
    }

    [Fact]
    public void Starts_on_the_menu_with_a_board_of_the_chosen_size()
    {
        GameSession session = NewSession(UserSettings.Default with { Map = new MapSize(18, 22) });

        Assert.Equal(SessionPhase.Menu, session.Phase);
        Assert.Equal(18, session.State.Rows);
        Assert.Equal(22, session.State.Columns);
    }

    [Fact]
    public void A_round_goes_through_countdown_then_playing()
    {
        GameSession session = NewSession();

        session.StartRound(2);
        Assert.Equal(SessionPhase.Countdown, session.Phase);
        Assert.Equal(2, session.PlayerCount);

        session.BeginPlaying();
        Assert.Equal(SessionPhase.Playing, session.Phase);
    }

    [Fact]
    public void Steering_is_ignored_during_the_countdown()
    {
        GameSession session = NewSession();
        session.StartRound(1);

        Assert.Equal(SessionCommand.None, session.HandleKey("W"));
        session.BeginPlaying();
        session.Tick();

        Assert.Equal(new Position(7, 4), session.State.HeadPosition());
    }

    [Theory]
    [InlineData("W")]
    [InlineData("Up")]
    public void With_one_player_both_key_sets_steer_the_snake(string key)
    {
        GameSession session = Playing();

        Assert.Equal(SessionCommand.Steered, session.HandleKey(key));
        session.Tick();

        Assert.Equal(new Position(6, 3), session.State.HeadPosition());
    }

    [Fact]
    public void With_two_players_the_arrows_steer_player_two()
    {
        GameSession session = Playing(players: 2);

        session.HandleKey("Up");
        session.Tick();

        Assert.Equal(new Position(5, 4), session.State.Players[0].Head);
        Assert.Equal(new Position(8, 11), session.State.Players[1].Head);
    }

    [Fact]
    public void S_steers_while_playing_and_takes_a_screenshot_while_paused()
    {
        GameSession session = Playing();

        Assert.Equal(SessionCommand.Steered, session.HandleKey("S"));
        Assert.Equal(SessionCommand.PauseToggled, session.HandleKey("P"));
        Assert.True(session.State.IsPaused);
        Assert.Equal(SessionCommand.Screenshot, session.HandleKey("S"));
        Assert.Equal(SessionCommand.None, session.HandleKey("A"));
    }

    [Fact]
    public void Fullscreen_works_everywhere_and_escape_goes_back_to_the_menu()
    {
        GameSession session = Playing();

        Assert.Equal(SessionCommand.ToggleFullscreen, session.HandleKey("F"));
        Assert.Equal(SessionCommand.BackToMenu, session.HandleKey("Escape"));
        Assert.Equal(SessionPhase.Menu, session.Phase);
        Assert.Equal(SessionCommand.ToggleFullscreen, session.HandleKey("F"));
        Assert.Equal(SessionCommand.None, session.HandleKey("Escape"));
    }

    [Fact]
    public void Rebound_keys_are_used()
    {
        var settings = UserSettings.Default with { Keys = KeyBindings.Default.Rebind(GameAction.Pause, "Space") };
        GameSession session = Playing(settings: settings);

        Assert.Equal(SessionCommand.None, session.HandleKey("P"));
        Assert.Equal(SessionCommand.PauseToggled, session.HandleKey("Space"));
    }

    [Fact]
    public void Hitting_the_wall_ends_the_round_with_game_over()
    {
        GameSession session = Playing();

        TickUntilOver(session);

        Assert.Equal(SessionPhase.GameOver, session.Phase);
        Assert.Equal("GAME OVER", session.LastResult!.Title);
        Assert.Equal("SCORE 0", session.LastResult.Detail);
        Assert.False(session.LastResult.IsNewRecord);
        Assert.Equal(SoundCue.Die, session.LastResult.Sound);
    }

    [Fact]
    public void Beating_the_record_is_reported_and_stored()
    {
        // First food right in front of the snake.
        GameSession session = Playing(foodIndex: 106);

        TickUntilOver(session);

        Assert.Equal("NEW HIGHEST SCORE", session.LastResult!.Title);
        Assert.True(session.LastResult.IsNewRecord);
        Assert.Equal(1, session.HighScore);
    }

    [Fact]
    public void Demo_mode_plays_by_itself_and_never_records()
    {
        GameSession session = Playing(demo: true, foodIndex: 106);
        Assert.True(session.IsBot(0));

        for (int i = 0; i < 200; i++)
        {
            session.Tick();
        }

        Assert.True(session.State.Score > 0 || session.Phase == SessionPhase.GameOver);
        TickUntilOver(session);
        Assert.Equal(0, session.HighScore);
    }

    [Fact]
    public void Pressing_a_direction_in_demo_mode_takes_over_the_snake()
    {
        GameSession session = Playing(demo: true);

        session.HandleKey("Up");

        Assert.False(session.IsBot(0));
    }

    [Fact]
    public void Two_player_round_announces_the_winner()
    {
        GameSession session = Playing(players: 2);
        session.HandleKey("W");

        TickUntilOver(session);

        Assert.Equal("PLAYER 2 WINS", session.LastResult!.Title);
        Assert.Equal("P1 0  :  0 P2", session.LastResult.Detail);
    }

    [Fact]
    public void Tick_interval_follows_the_speed_setting()
    {
        GameSession session = Playing(settings: UserSettings.Default with { Speed = SpeedLevel.Progressive }, foodIndex: 106);
        TimeSpan before = session.TickInterval;

        session.Tick();

        Assert.True(session.TickInterval < before);
    }

    [Fact]
    public void New_settings_rebuild_the_menu_board_at_once()
    {
        GameSession session = NewSession();

        session.ApplySettings(UserSettings.Default with { Map = new MapSize(20, 25), Walls = WallMode.Wrap });

        Assert.Equal(20, session.State.Rows);
        Assert.Equal(WallMode.Wrap, session.State.Options.Walls);
    }
}

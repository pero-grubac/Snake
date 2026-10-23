namespace Snake.Core;

public enum SessionPhase
{
    Menu,
    Countdown,
    Playing,
    GameOver
}

public enum SessionCommand
{
    None,
    Steered,
    PauseToggled,
    Screenshot,
    ToggleFullscreen,
    BackToMenu
}

/// <summary>What the game-over screen shows.</summary>
public sealed record RoundResult(string Title, string Detail, bool IsNewRecord, SoundCue Sound);

/// <summary>
/// The flow of the game around a <see cref="GameState"/>: menu, countdown, playing and game over,
/// key handling, speed, the demo bot and high scores. The window only draws it and keeps time.
/// </summary>
public sealed class GameSession
{
    private readonly Func<Random?> randomFactory;
    private readonly HashSet<int> botPlayers = new();

    public GameSession(UserSettings settings, HighScoreTable highScores, bool demo = false, Func<Random?>? randomFactory = null)
    {
        Settings = settings;
        HighScores = highScores;
        Demo = demo;
        this.randomFactory = randomFactory ?? (() => null);
        State = NewState(1);
    }

    public UserSettings Settings { get; private set; }
    public HighScoreTable HighScores { get; }

    /// <summary>Every snake starts under bot control and scores are never recorded.</summary>
    public bool Demo { get; }

    public SessionPhase Phase { get; private set; } = SessionPhase.Menu;
    public GameState State { get; private set; }
    public RoundResult? LastResult { get; private set; }

    public int PlayerCount => State.Players.Count;
    public int HighScore => HighScores.Get(Settings.Map);

    public TimeSpan TickInterval =>
        GameSpeed.TickInterval(Settings.Speed, State.Players.Max(player => player.Score));

    public bool IsBot(int player) => botPlayers.Contains(player);

    /// <summary>New settings take effect at once on the menu, otherwise from the next round.</summary>
    public void ApplySettings(UserSettings settings)
    {
        Settings = settings;
        if (Phase == SessionPhase.Menu)
        {
            State = NewState(1);
        }
    }

    public void ShowMenu()
    {
        Phase = SessionPhase.Menu;
        botPlayers.Clear();
        State = NewState(1);
    }

    public void StartRound(int players)
    {
        State = NewState(players);
        LastResult = null;
        botPlayers.Clear();
        if (Demo)
        {
            botPlayers.UnionWith(Enumerable.Range(0, players));
        }
        Phase = SessionPhase.Countdown;
    }

    public void BeginPlaying()
    {
        if (Phase == SessionPhase.Countdown)
        {
            Phase = SessionPhase.Playing;
        }
    }

    /// <param name="key">A WPF <c>Key</c> name, e.g. <c>"W"</c> or <c>"Escape"</c>.</param>
    public SessionCommand HandleKey(string key)
    {
        if (string.Equals(key, KeyBindings.ReservedKey, StringComparison.OrdinalIgnoreCase))
        {
            if (Phase == SessionPhase.Menu)
            {
                return SessionCommand.None;
            }
            ShowMenu();
            return SessionCommand.BackToMenu;
        }

        List<GameAction> actions = Settings.Keys.ActionsFor(key).ToList();
        if (actions.Contains(GameAction.Fullscreen))
        {
            return SessionCommand.ToggleFullscreen;
        }
        if (Phase != SessionPhase.Playing)
        {
            return SessionCommand.None;
        }
        if (actions.Contains(GameAction.Pause))
        {
            State.TogglePause();
            return SessionCommand.PauseToggled;
        }
        if (State.IsPaused)
        {
            return actions.Contains(GameAction.Screenshot) ? SessionCommand.Screenshot : SessionCommand.None;
        }

        bool steered = false;
        foreach (GameAction action in actions.Where(KeyBindings.IsMovement))
        {
            // With one player both key sets steer the same snake.
            int player = PlayerCount == 1 || action <= GameAction.P1Right ? 0 : 1;
            botPlayers.Remove(player);
            State.ChangeDirection(player, DirectionOf(action));
            steered = true;
        }
        return steered ? SessionCommand.Steered : SessionCommand.None;
    }

    public MoveResult Tick()
    {
        if (Phase != SessionPhase.Playing)
        {
            return MoveResult.None;
        }

        if (!State.IsPaused)
        {
            foreach (int bot in botPlayers)
            {
                State.ChangeDirection(bot, AutoPilot.NextDirection(State, bot));
            }
        }

        MoveResult result = State.Move();
        if (State.GameOver)
        {
            Phase = SessionPhase.GameOver;
            LastResult = BuildResult();
        }
        return result;
    }

    private RoundResult BuildResult()
    {
        if (PlayerCount == 1)
        {
            int score = State.Score;
            bool record = !Demo && HighScores.Submit(Settings.Map, score);
            string detail = $"SCORE {score}";
            if (State.IsWon)
            {
                return new RoundResult("YOU WIN!", detail, record, SoundCue.Win);
            }
            return record
                ? new RoundResult("NEW HIGHEST SCORE", detail, true, SoundCue.Record)
                : new RoundResult("GAME OVER", detail, false, SoundCue.Die);
        }

        string scores = $"P1 {State.Players[0].Score}  :  {State.Players[1].Score} P2";
        return State.Winner is int winner
            ? new RoundResult($"PLAYER {winner + 1} WINS", scores, false, SoundCue.Win)
            : new RoundResult("DRAW", scores, false, SoundCue.Die);
    }

    private GameState NewState(int players) =>
        new(Settings.Map, new GameOptions { PlayerCount = players, Walls = Settings.Walls }, randomFactory());

    private static Direction DirectionOf(GameAction action) => action switch
    {
        GameAction.P1Up or GameAction.P2Up => Direction.Up,
        GameAction.P1Down or GameAction.P2Down => Direction.Down,
        GameAction.P1Left or GameAction.P2Left => Direction.Left,
        _ => Direction.Right,
    };
}

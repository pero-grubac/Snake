using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Snake.Core;

namespace Snake
{
    /// <summary>
    /// Draws a <see cref="GameSession"/> and keeps its time. All rules live in Snake.Core.
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum Screen
        {
            None,
            Menu,
            Settings,
            Controls,
            Message
        }

        private static readonly string ScreenshotDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Blavor");

        private static readonly Dictionary<Direction, int> DirectionToRotation = new()
        {
            { Direction.Up, 0 },
            { Direction.Right, 90 },
            { Direction.Down, 180 },
            { Direction.Left, 270 }
        };

        private readonly HighScoreStore highScoreStore = new(Path.Combine(App.DataDirectory, "score.txt"));
        private readonly SettingsStore settingsStore = new(Path.Combine(App.DataDirectory, "settings.json"));
        private readonly SoundEffects sounds = new();
        private readonly GameSession session;
        private readonly ScaleTransform foodPulse = new(1, 1);

        private Image[,] gridImages = new Image[0, 0];
        private int lastPlayers = 1;
        private string pauseNote = string.Empty;

        /// <summary>Bumped whenever a round is abandoned, so its async loop stops.</summary>
        private int roundToken;

        public MainWindow()
        {
            InitializeComponent();
            UserSettings settings = LoadSettings();
            session = new GameSession(settings, LoadHighScores(), App.DemoMode);
            sounds.Enabled = settings.Sound;

            SettingsView.Saved += SettingsView_Saved;
            SettingsView.Cancelled += (_, _) => ShowMenu();
            ControlsView.Done += ControlsView_Done;

            StartFoodPulse();
            ShowMenu();
        }

        // ---------- Screens ----------

        private void ShowScreen(Screen screen)
        {
            Overlay.Visibility = screen == Screen.None ? Visibility.Collapsed : Visibility.Visible;
            MenuPanel.Visibility = Show(screen == Screen.Menu);
            SettingsView.Visibility = Show(screen == Screen.Settings);
            ControlsView.Visibility = Show(screen == Screen.Controls);
            MessagePanel.Visibility = Show(screen == Screen.Message);
            if (screen != Screen.Message)
            {
                ResultButtons.Visibility = Visibility.Collapsed;
            }
            UpdatePausedBadge();
        }

        private Screen CurrentScreen =>
            Overlay.Visibility != Visibility.Visible ? Screen.None
            : MenuPanel.IsVisible ? Screen.Menu
            : SettingsView.Visibility == Visibility.Visible ? Screen.Settings
            : ControlsView.Visibility == Visibility.Visible ? Screen.Controls
            : Screen.Message;

        private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

        private void ShowMenu()
        {
            roundToken++;
            if (session.Phase != SessionPhase.Menu)
            {
                session.ShowMenu();
            }
            Redraw();
            MenuBestText.Text = $"BEST ON {session.Settings.Map}: {session.HighScore}";
            ShowScreen(Screen.Menu);
            Dispatcher.BeginInvoke(() => OnePlayerButton.Focus());
        }

        private void Redraw()
        {
            SetupGridIfNeeded();
            Draw();
            UpdateHints();
        }

        // ---------- Menu buttons ----------

        private void OnePlayerButton_Click(object sender, RoutedEventArgs e) => StartRound(1);

        private void TwoPlayersButton_Click(object sender, RoutedEventArgs e) => StartRound(2);

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e) => StartRound(lastPlayers);

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            sounds.Play(SoundCue.Click);
            ShowMenu();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            sounds.Play(SoundCue.Click);
            ShowScreen(Screen.Settings);
            SettingsView.Load(session.Settings);
        }

        private void ControlsButton_Click(object sender, RoutedEventArgs e)
        {
            sounds.Play(SoundCue.Click);
            ShowScreen(Screen.Controls);
            ControlsView.Load(session.Settings.Keys);
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e) => Close();

        private void SettingsView_Saved(object? sender, UserSettings settings)
        {
            ApplySettings(settings);
            sounds.Play(SoundCue.Click);
            ShowMenu();
        }

        private void ControlsView_Done(object? sender, KeyBindings bindings)
        {
            ApplySettings(session.Settings with { Keys = bindings });
            sounds.Play(SoundCue.Click);
            ShowMenu();
        }

        private void ApplySettings(UserSettings settings)
        {
            session.ApplySettings(settings);
            sounds.Enabled = settings.Sound;
            Try(() => settingsStore.Save(settings), "Settings could not be saved");
        }

        // ---------- Round ----------

        private async void StartRound(int players)
        {
            lastPlayers = players;
            pauseNote = string.Empty;
            session.StartRound(players);
            int token = ++roundToken;
            Redraw();
            ShowScreen(Screen.Message);
            MessageDetail.Text = players == 2 ? "PLAYER 1 vs PLAYER 2" : session.Demo ? "DEMO" : string.Empty;
            Focus();

            for (int i = 3; i >= 1; i--)
            {
                ShowMessage(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sounds.Play(SoundCue.Countdown);
                await Task.Delay(600);
                if (token != roundToken)
                {
                    return;
                }
            }
            ShowMessage("GO!");
            sounds.Play(SoundCue.Go);
            await Task.Delay(350);
            if (token != roundToken)
            {
                return;
            }

            session.BeginPlaying();
            ShowScreen(Screen.None);
            Keyboard.ClearFocus();
            Focus();
            await GameLoop(token);
        }

        private async Task GameLoop(int token)
        {
            while (session.Phase == SessionPhase.Playing)
            {
                await Task.Delay(session.TickInterval);
                if (token != roundToken)
                {
                    return;
                }

                int[] scoresBefore = session.State.Players.Select(player => player.Score).ToArray();
                MoveResult result = session.Tick();
                if (result.HasFlag(MoveResult.Ate))
                {
                    sounds.Play(SoundCue.Eat);
                    for (int i = 0; i < scoresBefore.Length; i++)
                    {
                        if (session.State.Players[i].Score > scoresBefore[i])
                        {
                            Pop(i == 0 ? P1ScoreScale : P2ScoreScale, 1.45);
                        }
                    }
                }
                Draw();
            }

            if (session.Phase == SessionPhase.GameOver)
            {
                await ShowGameOver(token);
            }
        }

        private async Task ShowGameOver(int token)
        {
            RoundResult result = session.LastResult!;
            if (!session.State.IsWon)
            {
                sounds.Play(SoundCue.Die);
            }
            await DrawDeadSnakes();
            await Task.Delay(600);
            if (token != roundToken)
            {
                return;
            }

            if (result.IsNewRecord)
            {
                Try(() => highScoreStore.Save(session.HighScores), "High score could not be saved");
            }
            if (result.Sound != SoundCue.Die)
            {
                sounds.Play(result.Sound);
            }

            Draw();
            ShowScreen(Screen.Message);
            MessageText.FontSize = 40;
            MessageText.Text = result.Title;
            MessageText.Foreground = session.State.Winner == 1
                ? (Brush)FindResource("Accent2Brush")
                : (Brush)FindResource("LogoBrush");
            Pop(MessageScale, 1.3);
            MessageDetail.Text = result.Detail;
            ResultButtons.Visibility = Visibility.Visible;
            _ = Dispatcher.BeginInvoke(() => PlayAgainButton.Focus());
        }

        private void ShowMessage(string text)
        {
            MessageText.FontSize = 64;
            MessageText.Foreground = (Brush)FindResource("LogoBrush");
            MessageText.Text = text;
            Pop(MessageScale, 1.6);
        }

        // ---------- Input ----------

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ControlsView.IsCapturing)
            {
                return;
            }

            Key key = KeyNames.RealKey(e);
            Screen screen = CurrentScreen;
            if (screen is Screen.Settings or Screen.Controls)
            {
                if (key == Key.Escape)
                {
                    e.Handled = true;
                    ShowMenu();
                }
                return;
            }

            SessionCommand command = session.HandleKey(key.ToString());
            switch (command)
            {
                case SessionCommand.ToggleFullscreen:
                    ToggleFullScreen();
                    e.Handled = true;
                    break;
                case SessionCommand.BackToMenu:
                    ShowMenu();
                    e.Handled = true;
                    break;
                case SessionCommand.PauseToggled:
                    pauseNote = string.Empty;
                    sounds.Play(SoundCue.Click);
                    Draw();
                    break;
                case SessionCommand.Screenshot:
                    CaptureScreenshot();
                    break;
            }

            // While the snake moves, keys belong to the game, not to focus navigation.
            if (session.Phase is SessionPhase.Countdown or SessionPhase.Playing)
            {
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
        }

        // ---------- Drawing ----------

        private void SetupGridIfNeeded()
        {
            int rows = session.State.Rows;
            int columns = session.State.Columns;
            if (gridImages.GetLength(0) == rows && gridImages.GetLength(1) == columns)
            {
                return;
            }

            gridImages = new Image[rows, columns];
            GameGrid.Children.Clear();
            GameGrid.Rows = rows;
            GameGrid.Columns = columns;
            GameGrid.Width = GameGrid.Height * (columns / (double)rows);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Image image = new Image
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                    };
                    gridImages[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }
        }

        private void Draw()
        {
            GameState state = session.State;
            for (int r = 0; r < state.Rows; r++)
            {
                for (int c = 0; c < state.Columns; c++)
                {
                    bool food = state[r, c] == GridValue.Food;
                    gridImages[r, c].Source = food ? Images.Food : Images.Empty;
                    gridImages[r, c].RenderTransform = food ? foodPulse : Transform.Identity;
                }
            }

            foreach (Player player in state.Players)
            {
                SnakeSprites sprites = Images.ForPlayer(player.Index);
                bool dead = !player.IsAlive;
                foreach (Position cell in player.Body)
                {
                    gridImages[cell.Row, cell.Column].Source = dead ? sprites.DeadBody : sprites.Body;
                }

                Image head = gridImages[player.Head.Row, player.Head.Column];
                head.Source = dead ? sprites.DeadHead : sprites.Head;
                head.RenderTransform = new RotateTransform(DirectionToRotation[player.Direction]);
            }

            DrawStats();
        }

        private void DrawStats()
        {
            GameState state = session.State;
            bool twoPlayers = state.Players.Count == 2;
            P1Label.Text = twoPlayers ? "PLAYER 1" : "SCORE";
            P1ScoreText.Text = state.Players[0].Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
            P2Card.Visibility = Show(twoPlayers);
            if (twoPlayers)
            {
                P2ScoreText.Text = state.Players[1].Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            BestCard.Visibility = Show(!twoPlayers);
            BestText.Text = session.HighScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SpeedText.Text = SpeedName(session.Settings.Speed);

            string walls = state.Options.Walls == WallMode.Wrap ? "WRAP-AROUND WALLS" : "SOLID WALLS";
            string mode = twoPlayers ? "2 PLAYERS" : session.Demo ? "DEMO" : "1 PLAYER";
            ModeText.Text = $"{state.Rows}X{state.Columns} · {walls} · {mode}";
            UpdatePausedBadge();
        }

        private void UpdatePausedBadge()
        {
            bool paused = session.Phase == SessionPhase.Playing && session.State.IsPaused && Overlay.Visibility != Visibility.Visible;
            PausedBadge.Visibility = Show(paused);
            KeyBindings keys = session.Settings.Keys;
            PausedText.Text = pauseNote.Length > 0
                ? $"PAUSED · {pauseNote}"
                : $"PAUSED · {KeyNames.Display(keys[GameAction.Pause])} RESUME · {KeyNames.Display(keys[GameAction.Screenshot])} SCREENSHOT";
        }

        private void UpdateHints()
        {
            KeyBindings keys = session.Settings.Keys;
            string p1 = MoveKeys(keys, GameAction.P1Up, GameAction.P1Left, GameAction.P1Down, GameAction.P1Right);
            string p2 = MoveKeys(keys, GameAction.P2Up, GameAction.P2Left, GameAction.P2Down, GameAction.P2Right);
            string movement = session.PlayerCount == 2 && session.Phase != SessionPhase.Menu
                ? $"P1 {p1}  ·  P2 {p2}"
                : $"{p1} / {p2} MOVE";
            HintText.Text = $"{movement}   ·   {KeyNames.Display(keys[GameAction.Pause])} PAUSE   ·   "
                + $"{KeyNames.Display(keys[GameAction.Fullscreen])} FULLSCREEN   ·   ESC MENU";
        }

        private static string MoveKeys(KeyBindings keys, params GameAction[] actions) =>
            string.Concat(actions.Select(action => KeyNames.Display(keys[action])));

        private static string SpeedName(SpeedLevel speed) => speed switch
        {
            SpeedLevel.Slow => "SLOW",
            SpeedLevel.Fast => "FAST",
            SpeedLevel.Progressive => "SPEEDS UP",
            _ => "NORMAL",
        };

        private async Task DrawDeadSnakes()
        {
            foreach (Player player in session.State.Players.Where(player => !player.IsAlive))
            {
                SnakeSprites sprites = Images.ForPlayer(player.Index);
                int i = 0;
                foreach (Position position in player.Body)
                {
                    gridImages[position.Row, position.Column].Source = i++ == 0 ? sprites.DeadHead : sprites.DeadBody;
                    await Task.Delay(40);
                }
            }
        }

        // ---------- Animation ----------

        private void StartFoodPulse()
        {
            var pulse = new DoubleAnimation(0.82, 1.04, TimeSpan.FromMilliseconds(520))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            foodPulse.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            foodPulse.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        private static void Pop(ScaleTransform transform, double from)
        {
            var pop = new DoubleAnimation(from, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 },
            };
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        }

        // ---------- Files ----------

        private UserSettings LoadSettings()
        {
            try
            {
                return settingsStore.Load();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return UserSettings.Default;
            }
        }

        private HighScoreTable LoadHighScores()
        {
            try
            {
                return highScoreStore.Load();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"High scores could not be loaded:\n{e.Message}", "Blavor");
                return new HighScoreTable();
            }
        }

        private void Try(Action action, string failure)
        {
            try
            {
                action();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"{failure}:\n{e.Message}", "Blavor");
            }
        }

        private void CaptureScreenshot()
        {
            if (Content is not FrameworkElement content)
            {
                return;
            }

            // The badge is not part of the picture.
            PausedBadge.Visibility = Visibility.Collapsed;
            content.UpdateLayout();

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                (int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX),
                (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY),
                dpi.PixelsPerInchX,
                dpi.PixelsPerInchY,
                PixelFormats.Pbgra32);
            renderTarget.Render(content);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            string message;
            try
            {
                Directory.CreateDirectory(ScreenshotDirectory);
                string fileName = $"snake_{DateTime.Now:yyyyMMddHHmmss}.png";
                using (FileStream stream = new FileStream(Path.Combine(ScreenshotDirectory, fileName), FileMode.Create))
                {
                    encoder.Save(stream);
                }
                message = "SCREENSHOT SAVED";
                sounds.Play(SoundCue.Record);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                message = "SCREENSHOT FAILED";
            }

            pauseNote = message;
            UpdatePausedBadge();
        }
    }
}
